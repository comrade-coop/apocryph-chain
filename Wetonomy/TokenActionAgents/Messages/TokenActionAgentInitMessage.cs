using Apocryph.FunctionApp.Agent;
using System;
using System.Collections.Generic;
using Wetonomy.TokenActionAgents.State;

namespace Wetonomy.TokenActionAgents.Messages
{
    public class TokenActionAgentInitMessage<T> where T: IEquatable<T>
    {
        public AgentCapability TokenManagerAgentCapability { get; set; }
        public Dictionary<(string, Type), TriggerMessage<T>> TriggererToAction { get; set; }

        public TokenActionAgentInitMessage(
            AgentCapability tokenManagerAgentCapability,
            Dictionary<(string, Type), TriggerMessage<T>> triggererToAction)
        {
            TokenManagerAgentCapability = tokenManagerAgentCapability;
            TriggererToAction = triggererToAction;
        }
    }
}