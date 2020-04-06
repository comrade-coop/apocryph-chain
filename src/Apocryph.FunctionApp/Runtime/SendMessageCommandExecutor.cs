using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Apocryph.FunctionApp.Agent;
using Apocryph.FunctionApp.Command;
using Apocryph.FunctionApp.Model;
using Apocryph.FunctionApp.Ipfs;
using Ipfs;
using Ipfs.Http;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace Apocryph.FunctionApp
{
    public static class SendMessageCommandExecutor
    {
        public class State
        {
            public Dictionary<string, IHashed<ValidatorSet>> ValidatorSets { get; set; }
        }

        [FunctionName(nameof(SendMessageCommandExecutor))]
        public static async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("agentId")] string agentId,
            [Perper("ipfsGateway")] string ipfsGateway,
            [PerperStream("validatorSetsStream")] IAsyncEnumerable<Dictionary<string, IHashed<ValidatorSet>>> validatorSetsStream,
            [PerperStream("commandsStream")] IAsyncEnumerable<SendMessageCommand> commandsStream,
            CancellationToken cancellationToken)
        {
            var state = await context.FetchStateAsync<State>() ?? new State();

            await Task.WhenAll(
                validatorSetsStream.ForEachAsync(async validatorSets =>
                {
                    state.ValidatorSets = validatorSets;
                    await context.UpdateStateAsync(state);
                }, cancellationToken),

                commandsStream.ForEachAsync(async command =>
                {
                    var notification = new CallNotification
                    {
                        From = agentId,
                        Command = command,
                        // Step = TODO,
                        // ValidatorSet = TODO,
                        // Commits = TODO
                    };

                    await context.CallWorkerAsync<object>(nameof(PBFTNotificationWorker), new
                    {
                        agentId = command.Target,
                        validatorSet = state.ValidatorSets[command.Target],
                        notification = notification,
                        ipfsGateway
                    }, cancellationToken);
                }, cancellationToken));
        }
    }
}