using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Executor.Agent.Calls;

public static class Register
{
    public static async Task RunAsync(string agentCodeHash, PerperAgent agent, string agentFunction)
    {
        await PerperState.SetAsync(agentCodeHash, (agent, agentFunction));
    }
}