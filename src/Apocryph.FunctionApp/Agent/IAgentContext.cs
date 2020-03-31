using System;

namespace Apocryph.FunctionApp.Agent
{
    public interface IAgentContext
    {
        AgentCapability IssueCapability(string[] messageTypes);
        void RevokeCapability(AgentCapability capability);

        AgentCallTicket RequestCallTicket(AgentCapability agent);

        void CreateAgent(string id, string functionName, object initMessage, AgentCallTicket callTicket);
        void SendMessage(AgentCapability receiver, object message, AgentCallTicket callTicket);

        void AddReminder(TimeSpan time, object data);
        void MakePublication(object payload);
        void AddSubscription(string target);

        void SendServiceMessage(string command, object parameters);
    }

    public interface IAgentContext<out T> : IAgentContext
    {
        T State { get; }
    }
}