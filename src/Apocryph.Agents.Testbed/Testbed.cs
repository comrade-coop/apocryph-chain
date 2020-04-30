using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Apocryph.Agents.Testbed.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Perper.WebJobs.Extensions.Model;

namespace Apocryph.Agents.Testbed
{
    public class Testbed
    {
        private readonly ILogger<Testbed> _logger;

        public Testbed(ILogger<Testbed> logger)
        {
            _logger = logger;
        }
        //Setup(context, "ManyAgentsTest", "Runtime", "Monitor", cancellationToken);
        public async Task Setup(PerperStreamContext context, string agentDelegate, string runtimeDelegate, string monitorDelegate, string serverDelegate,
            CancellationToken cancellationToken)
        {
            var runtime = context.DeclareStream(runtimeDelegate);
            await context.StreamFunctionAsync(runtime, new { agentDelegate, serverDelegate, commands = runtime });
            await context.StreamActionAsync(monitorDelegate, new { commands = runtime });
            await context.BindOutput(cancellationToken);
        }

        public async Task Agent<T>(Func<object, AgentCapability, object, Task<AgentContext<T>>> entryPoint,
            string agentId,
            object initMessage,
            IAsyncEnumerable<AgentCommands> commands, IAsyncCollector<AgentCommands> output,
            ICollection<object> state,
            CancellationToken cancellationToken) where T : class
        {
            await Task.WhenAll(
                InitAgent(entryPoint, state, agentId, initMessage, output),
                ExecuteAgent(entryPoint, state, agentId, commands, output, cancellationToken));
        }

        public async Task Runtime(PerperStreamContext context, string agentDelegate, string serverDelegate,
            IAsyncEnumerable<AgentCommands> commands, CancellationToken cancellationToken)
        {
            var agents = new List<IAsyncDisposable>();
            var agentState = new Dictionary<string, List<object>>();

            var server = await context.StreamFunctionAsync(serverDelegate, new { agentState, commands = context.GetStream() });
            agents.Add(server);

            await Task.WhenAll(
                InitRuntime(context, agentDelegate, agentState, agents),
                ExecuteRuntime(context, commands, agents, agentState, cancellationToken));
        }

        public async Task Monitor(IAsyncEnumerable<AgentCommands> commands,
            CancellationToken cancellationToken)
        {
            

            await foreach (var commandsBatch in commands.WithCancellation(cancellationToken))
            {
                foreach (var command in commandsBatch.Commands)
                {   
                    _logger.LogInformation($"{command.CommandType.ToString()} command with {command.Receiver?.Issuer} receiver");
                }
            }
        }

        public async Task ExecuteClientServer(IAsyncEnumerable<AgentCommands> commands, IAsyncCollector<AgentCommands> output,
            IDictionary<string, List<object>> _, CancellationToken cancellationToken)
        {

            var host = CreateHost();

            var clientService = host.Services.GetService(typeof(ClientService)) as ClientService;
            clientService.Output = output;

            Thread newThread = new Thread(() => RunHost(host));
            newThread.Start();

            await foreach (var commandsBatch in commands.WithCancellation(cancellationToken))
            {
                if (!clientService.AgentStates.ContainsKey(commandsBatch.Origin))
                {
                    clientService.AgentStates.Add(commandsBatch.Origin, commandsBatch.State);
                }
                clientService.AgentStates[commandsBatch.Origin] = commandsBatch.State;

                foreach (var command in commandsBatch.Commands)
                {
                    if (command.CommandType == AgentCommandType.Publish)
                    {
                        var str = command.Message.ToString();
                        string serialized = JsonConvert.SerializeObject(command);
                        var msgBytes = Encoding.UTF8.GetBytes(serialized);
                        var tasks = new List<Task>();

                        foreach (var client in clientService.WebSocketClients)
                        {
                            Task current = client.SendAsync(new ArraySegment<byte>(msgBytes, 0, msgBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                            tasks.Add(current);
                        }
                        await Task.WhenAll(tasks);
                    }
                }
            }
        }

        public void RunHost(IHost host)
        {
            host.Run();
        }

        public IHost CreateHost()
        {
            return Host.CreateDefaultBuilder().ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<WebSocketServerStartup>();
                }).Build();
        }


        private async Task InitRuntime(PerperStreamContext context, string agentDelegate, IDictionary<string, List<object>> agentState,
            ICollection<IAsyncDisposable> agents)
        {
            await Task.Delay(TimeSpan.FromSeconds(1)); //Wait for Execute to engage Runtime

            var state = new List<object>();
            var agent = await context.StreamFunctionAsync(agentDelegate, new
            {
                agentId = "AgentRoot",
                initMessage = new AgentRootInitMessage(),
                commands = context.GetStream(),
                state
            });
            agents.Add(agent);

            
            agentState.Add("AgentRoot", state);
            await context.RebindOutput(agents);
        }

        private async Task ExecuteRuntime(PerperStreamContext context, IAsyncEnumerable<AgentCommands> commands, 
            ICollection<IAsyncDisposable> agents, IDictionary<string, List<object>> agentState, CancellationToken cancellationToken)
        {
            await foreach (var commandsBatch in commands.WithCancellation(cancellationToken))
            {
                foreach (var command in commandsBatch.Commands)
                {
                    if (command.CommandType == AgentCommandType.CreateAgent)
                    {
                        var state = new List<object>();
                        var agent = await context.StreamFunctionAsync(command.Agent, new
                        {
                            agentId = command.AgentId,
                            initMessage = command.Message,
                            commands = context.GetStream(),
                            state
                        });
                        agents.Add(agent);
                        
                        agentState.Add(command.AgentId, state);
                    }
                }
                await context.RebindOutput(agents);
            }
        }

        private async Task InitAgent<T>(Func<object, AgentCapability, object, Task<AgentContext<T>>> entryPoint,
            ICollection<object> states,
            string agentId,
            object initMessage, IAsyncCollector<AgentCommands> output) where T: class
        {
            await Task.Delay(TimeSpan.FromSeconds(1)); //Wait for Execute to engage Runtime

            var agentContext = await entryPoint(
                null,
                new AgentCapability(agentId, new[] {initMessage.GetType()}),
                initMessage);
            states.Add(agentContext.InternalState);
            await output.AddAsync(agentContext.GetCommands());
        }

        private async Task ExecuteAgent<T>(Func<object, AgentCapability, object, Task<AgentContext<T>>> entryPoint,
            ICollection<object> states,
            string agentId,
            IAsyncEnumerable<AgentCommands> commands, IAsyncCollector<AgentCommands> output,
            CancellationToken cancellationToken) where T : class
        {
            var publishers = new HashSet<string>();
            await foreach (var commandsBatch in commands.WithCancellation(cancellationToken))
            {
                foreach (var command in commandsBatch.Commands)
                {
                    if (command.CommandType == AgentCommandType.SendMessage && command.Receiver.Issuer == agentId)
                    {
                        var agentContext = await entryPoint(states.Last(), command.Receiver, command.Message);
                        states.Add(agentContext.InternalState);
                        await output.AddAsync(agentContext.GetCommands(), cancellationToken);
                    }
                    else if (command.CommandType == AgentCommandType.Reminder && commandsBatch.Origin == agentId)
                    {
                        await Task.Delay(command.Timeout, cancellationToken);
                        var agentContext = await entryPoint(states.Last(), command.Receiver, command.Message);
                        states.Add(agentContext.InternalState);
                        await output.AddAsync(agentContext.GetCommands(), cancellationToken);
                    }
                    else if (command.CommandType == AgentCommandType.Subscribe && commandsBatch.Origin == agentId)
                    {
                        publishers.Add(command.AgentId);
                    }
                    else if (command.CommandType == AgentCommandType.Publish && publishers.Contains(commandsBatch.Origin))
                    {
                        var agentContext = await entryPoint(states.Last(), new AgentCapability(agentId, typeof(string)), command.Message);
                        states.Add(agentContext.InternalState);
                        await output.AddAsync(agentContext.GetCommands(), cancellationToken);
                    }
                }
            }
        }
    }
}