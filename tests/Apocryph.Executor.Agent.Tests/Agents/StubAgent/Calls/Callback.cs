using Apocryph.Consensus;

namespace Apocryph.Executor.Agent.Tests.Agents.StubAgent.Calls
{
    public static class Callback
    {
        public static (AgentState, Message[]) Run(Chain chainId, AgentState inputState, Message[] inputMessage) => (inputState, inputMessage);
    }
}