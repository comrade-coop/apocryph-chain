using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace Apocryph.Runtime.FunctionApp.TestingStreams
{
    public class MainModule
    {
        [FunctionName(nameof(MainModule))]
        public async Task Run([PerperModuleTrigger(RunOnStartup = false)] PerperModuleContext context,
            CancellationToken cancellationToken)
        {
            /*var loop = context.DeclareStream(typeof(LoopStream));
            await context.StreamActionAsync(loop, new
            {
                self = loop.Subscribe()
            });

            await Task.Delay(1000);*/

            /*/
            var peering = context.DeclareStream(typeof(PeeringStream));

            var fakePeering = await context.StreamActionAsync(typeof(EchoStream), new
            {
                peering = peering.Subscribe()
            });

            var factory = await context.StreamFunctionAsync(typeof(FactoryStream), new
            {
                peering = fakePeering
            });

            await context.StreamActionAsync(peering, new
            {
                factory = factory.Subscribe(),
                filter = typeof(FactoryStream)
            });
            /*/

            var factory = await context.StreamActionAsync(typeof(FactoryStream), new
            {
            });

            /**/

            await context.BindOutput(cancellationToken);
        }
    }

    public class FactoryStream
    {
        [FunctionName(nameof(FactoryStream))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            CancellationToken cancellationToken)
        {
            var output = new List<IPerperStream>();
            for (var i = 0; i < 5; i++)
            {
                var client = await context.StreamFunctionAsync(typeof(ClientStream), new
                {
                    id = i,
                    peering = context.GetStream().Subscribe(),
                });

                output.Add(client);
                await context.RebindOutput(output);
            }

            await context.BindOutput(output, cancellationToken);
        }
    }

    public class ClientStream
    {
        [FunctionName(nameof(ClientStream))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("id")] int id,
            [Perper("peering")] IAsyncEnumerable<int> peering,
            [Perper("output")] IAsyncCollector<int> output,
            CancellationToken cancellationToken)
        {
            await Task.WhenAll(Task.Run(async () =>
                {
                    await Task.Delay(100);
                    await output.AddAsync(id);
                }),
                Task.Run(async () =>
                {
                    await foreach(var i in peering.WithCancellation(cancellationToken))
                    {
                        if (i == id) break;
                    }
                }));

            Console.WriteLine("All done!!");

            await context.BindOutput(cancellationToken);
        }
    }

    public class EchoStream
    {
        [FunctionName(nameof(EchoStream))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("peering")] IAsyncEnumerable<object> peering,
            [Perper("output")] IAsyncCollector<object> output,
            CancellationToken cancellationToken)
        {
            await foreach(var i in peering.WithCancellation(cancellationToken))
            {
                await output.AddAsync(i);
            }
        }
    }

    public class EchoBindStream
    {
        [FunctionName(nameof(EchoBindStream))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("peering")] IPerperStream peering,
            CancellationToken cancellationToken)
        {
            await context.BindOutput(peering, cancellationToken);
        }
    }

    public class LoopStream
    {
        [FunctionName(nameof(LoopStream))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("self")] IAsyncEnumerable<int> self,
            [Perper("output")] IAsyncCollector<int> output,
            CancellationToken cancellationToken)
        {
            await Task.WhenAll(Task.Run(async () =>
                {
                    await Task.Delay(100);
                    Console.WriteLine(-1);
                    await output.AddAsync(0);
                }),
                Task.Run(async () =>
                {
                    await foreach(var i in self.WithCancellation(cancellationToken))
                    {
                        Console.WriteLine(i);
                        await output.AddAsync(i + 1);
                        await Task.Delay(500);
                    }
                }));
        }
    }
}