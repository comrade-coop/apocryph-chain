using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Apocryph.Agents.Testbed
{
	public class WebSocketServerStartup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
            services.AddSingleton<WebSocketService>();
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
			
			app.Use(async (context, next) =>
			{
				if (context.Request.Path == "/ws")
				{
                    var buffer = new byte[1024 * 8];

                    if (context.WebSockets.IsWebSocketRequest)
					{
						WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        var webSocketService = context.RequestServices.GetService(typeof(WebSocketService)) as WebSocketService;
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

			});
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
