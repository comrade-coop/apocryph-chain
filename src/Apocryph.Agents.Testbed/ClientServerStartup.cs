using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Apocryph.Agents.Testbed.Api;
using Apocryph.Agents.Testbed.Agent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Apocryph.Agents.Testbed
{
    public class WebSocketServerStartup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
            services.AddSingleton<ClientService>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}


			var webSocketOptions = new WebSocketOptions()
			{
				KeepAliveInterval = TimeSpan.FromSeconds(120),
				ReceiveBufferSize = 8 * 1024
			};

			app.UseWebSockets(webSocketOptions);
			
			app.Use(WebSocketMiddleware);

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/query/{agentId}", ResponseAgentState);

                endpoints.MapPost("/action", SendAvtionToAgent);
            });
        }

        private async Task WebSocketMiddleware(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path == "/ws")
            {
                var buffer = new byte[1024 * 8];

                if (context.WebSockets.IsWebSocketRequest)
                {
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var webSocketService = context.RequestServices.GetService(typeof(ClientService)) as ClientService;
                    webSocketService.WebSocketClients.Add(webSocket);
                    await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            }
            else
            {
                await next();
            }
        }

        private async Task ResponseAgentState(HttpContext context)
        {
            var clientService = context.RequestServices.GetService(typeof(ClientService)) as ClientService;
            var name = context.Request.RouteValues["agentId"];


            if (clientService.AgentStates.ContainsKey(name.ToString()))
            {
                var agentState = clientService.AgentStates[name.ToString()];
                string serialized = JsonConvert.SerializeObject(agentState);
                await context.Response.WriteAsync(serialized);
            }
            else
            {
                await context.Response.WriteAsync($"No such contract as \"{name}\"");
            }
        }

        private async Task SendAvtionToAgent(HttpContext context)
        {
            var clientService = context.RequestServices.GetService(typeof(ClientService)) as ClientService;
            var output = clientService.Output;
            try
            {
                string json = await new StreamReader(context.Request.Body).ReadToEndAsync();
                var messageWrapper = JsonConvert.DeserializeObject<ActionWrapper>(json);
                var cmd = new AgentCommand()
                {
                    CommandType = AgentCommandType.SendMessage,
                    Message = new ActionWrapper(messageWrapper.MessageType, messageWrapper.Message.ToString()),
                    Receiver = new AgentCapability("AgentRoot", typeof(int))
                };

                var cmds = new AgentCommands { Origin = "ClientServer", Commands = new AgentCommand[] { cmd } };
                await output.AddAsync(cmds);
                await context.Response.WriteAsync($"Action Send");
            }
            catch(Exception e)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(e.Message);
            }
            //MUST GET ORIGIN IN WRAPPER

        }

        
        //private async Task Echo(HttpContext context, WebSocket webSocket)
        //{
        //	var buffer = new byte[1024 * 8];

        //	WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //	while (!result.CloseStatus.HasValue)
        //	{
        //		await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

        //		result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //	}
        //	await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        //}
    }
}
