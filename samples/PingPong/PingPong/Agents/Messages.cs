using Apocryph.Model;

namespace PingPong.Agents
{
    public record HitTheBallMessage(AgentReference Callback, string Content, int AccumulatedValue);
}