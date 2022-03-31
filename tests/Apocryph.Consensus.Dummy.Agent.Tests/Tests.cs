using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Apocryph.Ipfs;
using Apocryph.Ipfs.Fake;
using Apocryph.Ipfs.MerkleTree;
using Apocryph.KoTH;
using Apocryph.Testing;
using NUnit.Framework;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Consensus.Dummy.Agent.Tests;
// jaki input?
// producer 1 PerperStream AgentMessages,
// producer 2 PerperStream KothStates


public class Tests
{
    [Test]
    public async Task Test1()
    {
        var hashResolver = new FakeHashResolver();
        //var (chain, inputMessages, _) = await GetTestAgentScenario(hashResolver, "-", null, 1);
        //var chainId = await hashResolver.StoreAsync(chain);
        //var agentStates = await chain.GenesisState.AgentStates.EnumerateItems(hashResolver).ToDictionaryAsync(x => x.Nonce, x => x);
        //var executorAgent = await PerperContext.StartAgentAsync("Executor");
        //var callbackAgent = await PerperContext.StartAgentAsync("Callback");
        //await executorAgent.CallAsync("Register", Hash.From("Callback").ToString(), callbackAgent, "Callback");

        TestHelper.RunInContextOf("AgentLauncher", async () =>
        {


            await Task.Delay(10000);
            Console.Out.WriteLine("s");
            //var consensusStream = await consensus.CallAsync<PerperStream>("Consensus", executionStreamArgs);

            //await messages.ToListAsync();


            //var resposne = await consensusStream.EnumerateAsync<AgentMessage>().ToListAsync();
        });
    }

    public static async Task<(Chain chain, AgentMessage[] input, AgentMessage[] output)> GetTestAgentScenario(IHashResolver hashResolver, string consensusType, Hash<object>? consensusParameters, int slotsCount)
    {
        var messageFilter = new string[] { typeof(int).FullName! };
        var fakeChainId = Hash.From("123").Cast<Chain>();

        var agentStates = new[] {
            new AgentState(0, AgentReferenceData.From(new AgentReference(fakeChainId, 0, messageFilter)), Hash.From("AgentInc")),
            new AgentState(1, AgentReferenceData.From(new AgentReference(fakeChainId, 1, messageFilter)), Hash.From("AgentDec"))
        };

        var agentStatesTree = await MerkleTreeBuilder.CreateRootFromValues(hashResolver, agentStates, 2);

        var chain = new Chain(new ChainState(agentStatesTree, agentStates.Length), consensusType, consensusParameters, slotsCount);

        var chainId = await hashResolver.StoreAsync(chain);

        var inputMessages = new AgentMessage[]
        {
            new AgentMessage(new AgentReference(chainId, 0, messageFilter), AgentReferenceData.From(4)),
            new AgentMessage(new AgentReference(chainId, 1, messageFilter), AgentReferenceData.From(3)),
        };

        var expectedOutputMessages = new AgentMessage[]
        {
            new AgentMessage(new AgentReference(fakeChainId, 0, messageFilter), AgentReferenceData.From(5)),
            new AgentMessage(new AgentReference(fakeChainId, 1, messageFilter), AgentReferenceData.From(2)),
        };

        return (chain, inputMessages, expectedOutputMessages);

    }
}