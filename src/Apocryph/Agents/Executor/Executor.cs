using Apocryph.Ipfs;
using Apocryph.Model;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Agents.Executor;

/// <summary>
/// Agent responsible for registration and execution of others agents calls
/// </summary>
public class Executor
{
    /// <summary>
    /// Call registering agent's function name for given agent codeHash
    /// </summary>
    /// <param name="agentCodeHash">agent's codeHash</param>
    /// <param name="handlerAgent">agent's handler</param>
    /// <param name="handlerFunction">agent's function name</param>
    public async Task Register(Hash<string> agentCodeHash, PerperAgent handlerAgent, string handlerFunction)
    {
        var key = $"{agentCodeHash}";
        await PerperState.SetAsync(key, (handlerAgent, handlerFunction));
    }

    /// <summary>
    /// Call executing agent's function with agent message
    /// </summary>
    /// <param name="chainId">chain identification hash</param>
    /// <param name="agentState">agent state</param>
    /// <param name="agentMessage">message</param>
    /// <returns>Tuple of AgentState and AgentMessage array</returns>
    public async Task<(AgentState, AgentMessage[])> Execute(Hash<Chain> chainId, AgentState agentState, AgentMessage agentMessage)
    {
        var key = $"{agentState.CodeHash}";
        var (notNull, (handlerAgent, handlerFunction)) = await PerperState.TryGetAsync<(PerperAgent, string)>(key);

        if (notNull)
        {
            return await handlerAgent.CallAsync<(AgentState, AgentMessage[])>(handlerFunction, chainId, agentState, agentMessage);
        }

        return new(null!, null!);
    }
}