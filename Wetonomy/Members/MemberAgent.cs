using System;
using System.Collections.Generic;
using Apocryph.FunctionApp.Agent;
using Wetonomy.Members.Messages;

namespace Wetonomy.Members
{
    public static class MemberAgent
    {
        public class MemberState
        {
            public HashSet<string> Groups = new HashSet<string>();
            public Dictionary<object, AgentCapability> Capabilities = new Dictionary<object, AgentCapability>();
        }

        public static void Run(IAgentContext<MemberState> context, string sender, object message)
        {
            switch (message)
            {
                case AddGroupMessage addGroupMessage:
                    context.State.Groups.Add(addGroupMessage.GroupAgentId);
                    
                    break;

                case RemoveMemberMessage removeGroupMessage:
                    context.State.Groups.Remove(removeGroupMessage.GroupAgentId);
                    
                    break;
            }
        }
    }
}
