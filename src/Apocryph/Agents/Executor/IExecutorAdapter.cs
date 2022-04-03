using Apocryph.Ipfs;
using Apocryph.Model;
using Perper.Extensions;
using Perper.Model;
using PerperState = Perper.Extensions.PerperState;

namespace Apocryph.Agents.Executor;

public interface IExecutorAdapter
{
    Task SetHandlerAgent(string key, PerperAgent handlerAgent, string handlerFunction)
    {
        return PerperState.SetAsync(key, (handlerAgent, handlerFunction));
    }
    
    Task<(bool, (PerperAgent, string))> GetHandlerAgent(string key)
    {
        return PerperState.TryGetAsync<(PerperAgent, string)>(key);
    }
    
    Task<(AgentState, AgentMessage[])> HandlerAgentHandlerFunctionCall(PerperAgent handlerAgent, string handlerFunction, Hash<Chain> chainId, AgentState agentState, AgentMessage agentMessage)
    {
        return handlerAgent.CallAsync<(AgentState, AgentMessage[])>(handlerFunction, chainId, agentState, agentMessage);
    }
}