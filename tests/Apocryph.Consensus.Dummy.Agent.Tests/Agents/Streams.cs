using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Perper.Extensions;
using Perper.Model;

// ReSharper disable once CheckNamespace
namespace Apocryph.Consensus.Dummy.Agent.Tests.Agents.AgentLauncher.Streams;

public class AgentMessageProducer
{
    public async IAsyncEnumerable<string> RunAsync()
    {
        for (int i = 0; i < 10; i++)
        {
            yield return i.ToString();

            await Console.Out.WriteLineAsync($"AgentMessageProducer: {i}");
            await Task.Delay(1000);
        }
    }
}

public class AgentMessageProcessor
{
    public async IAsyncEnumerable<string> RunAsync(PerperStream generator)
    {
        await foreach (var message in generator.EnumerateAsync<string>())
        {
            await Console.Out.WriteLineAsync($"AgentMessageProcessor: {message}");
            yield return message;
        }
    }
}

public class AgentMessageConsumer
{
    public async Task RunAsync(PerperStream input)
    {
        await foreach (var message in input.EnumerateAsync<string>())
        {
            await Console.Out.WriteLineAsync($"AgentMessageConsumer: {message}");
        }
    }
}