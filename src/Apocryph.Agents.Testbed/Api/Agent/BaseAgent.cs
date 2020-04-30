using Apocryph.Agents.Testbed.Api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Apocryph.Agents.Testbed.Agent
{
    public class BaseState
    {
        public HashSet<AgentCapability> CapabilitiesForAgent;

        public HashSet<AgentCapability> SelfIssuedCapabilities;

        public BaseState()
        {
            CapabilitiesForAgent = new HashSet<AgentCapability>();
        }

        public bool CapabilityProtect(AgentCapability capability)
        {
            return SelfIssuedCapabilities.Contains(capability);
        }
    }

    public abstract class BaseAgent
    {
        public virtual object DeserializeActionWrapper(ActionWrapper wrapped)
        {
            var messageType = Type.GetType(wrapped.MessageType);
            return JsonConvert.DeserializeObject(Convert.ToString(wrapped.Message), messageType);
        }
    }
}
