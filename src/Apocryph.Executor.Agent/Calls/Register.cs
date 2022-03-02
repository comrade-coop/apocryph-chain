using Apocryph.Ipfs;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Executor.Agent.Calls;

public static class Register
{
    public static async Task RunAsync(string agentCodeHash, PerperAgent handlerAgent, string handlerFunction)
    {
        await PerperState.SetAsync(agentCodeHash, (handlerAgent, handlerFunction));
    }
}