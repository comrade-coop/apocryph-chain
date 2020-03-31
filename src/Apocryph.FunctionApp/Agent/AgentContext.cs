using System;
using System.Collections.Generic;
using Apocryph.FunctionApp.Command;

namespace Apocryph.FunctionApp.Agent
{
    public class AgentContext<T> : IAgentContext<T>
    {
        public List<ICommand> Commands { get; } = new List<ICommand>();

        public AgentContext(T state)
        {
            State = state;
        }

        public T State { get; }

        public AgentCapability IssueCapability(string[] messageTypes)
        {
            throw new NotImplementedException();
        }

        public void RevokeCapability(AgentCapability capability)
        {
            throw new NotImplementedException();
        }

        public AgentCallTicket RequestCallTicket(AgentCapability agent)
        {
            throw new NotImplementedException();
        }

        public void CreateAgent(string id, string functionName, object initMessage, AgentCallTicket callTicket)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(AgentCapability receiver, object message, AgentCallTicket callTicket)
        {
            // HACK: Should add actual Capability support in the Runtime
            Commands.Add(new SendMessageCommand{Target = receiver.AgentId, Payload = message});
        }

        public void AddReminder(TimeSpan time, object data)
        {
            Commands.Add(new ReminderCommand{Time = time, Data = data});
        }

        public void MakePublication(object payload)
        {
            Commands.Add(new PublicationCommand{Payload = payload});
        }

        public void AddSubscription(string target)
        {
            Commands.Add(new SubscriptionCommand{Target = target});
        }

        public void SendServiceMessage(string service, object parameters)
        {
            Commands.Add(new ServiceCommand{Service = service, Parameters = parameters});
        }
    }
}