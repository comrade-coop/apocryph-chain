using Apocryph.Model;

namespace SampleAgents.Agents
{
    public class PingPongMessage
    {
        public AgentReference Callback { get; set; }
        public string Content { get; set; }
        public int AccumulatedValue { get; set; }

        public PingPongMessage(AgentReference callback, string content, int accumulatedValue)
        {
            Callback = callback;
            Content = content;
            AccumulatedValue = accumulatedValue;
        }
    }
}