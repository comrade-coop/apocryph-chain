using Apocryph.Agents.Testbed.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apocryph.Agents.Testbed.Core
{
    public abstract class ForwardingAgent
    {
        public class BaseState
        {
            public HashSet<AgentCapability> CapabilitiesForAgent;

            public BaseState()
            {
                CapabilitiesForAgent = new HashSet<AgentCapability>();
            }
        }

        public Task<AgentContext<BaseState>> Run(BaseState state, AgentCapability self, object message)
        {
            if (state == null) throw new ArgumentNullException();
            var context = new AgentContext<BaseState>(state, self);
            

            if(message is ForwardMessage forwardMsg)
            {
                var capability = new AgentCapability(forwardMsg.Target, message.GetType());
                AgentCapability target;
                context.State.CapabilitiesForAgent.TryGetValue(capability, out target);
                context.ForwardMessage(target, forwardMsg.Message, null);
            }

            return Task.FromResult(context);
        }
    }
}
