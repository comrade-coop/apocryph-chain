using System.Runtime.CompilerServices;
using Apocryph.Ipfs;
using Apocryph.KoTH;
using Microsoft.Azure.WebJobs;

namespace Apocryph.Consensus.Snowball.Agent
{
    public class SnowballStream2
    {


        [FunctionName("SnowballStream")]
        public async IAsyncEnumerable<Message> SnowballStream([PerperTrigger] (
                Hash<Chain> self,
                IAgent executor) input,

            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {

        }

        [FunctionName("MessagePool")]
        public async Task MessagePool([PerperTrigger] (IAsyncEnumerable<Message> inputMessages, Hash<Chain> self) input)
        {
            await foreach (var message in input.inputMessages)
            {
                if (!message.Target.AllowedMessageTypes.Contains(message.Data.Type)) // NOTE: Should probably get handed by routing/execution instead
                    continue;

                var messagePool = await _state.GetValue<List<Message>>("messagePool");
                messagePool.Add(message);
                await _state.SetValue("messagePool", messagePool);
            }
            await _state.SetValue("finished", true); // DEBUG: Used for testing purposes mainly
        }

        [FunctionName("KothProcessor")]
        public async Task KothProcessor([PerperTrigger] (Hash<Chain> chain, IAsyncEnumerable<(Hash<Chain>, Slot?[])> kothStates) input)
        {
            await foreach (var (chain, slots) in input.kothStates)
            {
                if (chain != input.chain)
                    continue;

                var peers = slots.Where(s => s != null).Select(s => s!.Peer).ToArray();

                await _state.SetValue("kothPeers", peers);
            }
        }
    }
}