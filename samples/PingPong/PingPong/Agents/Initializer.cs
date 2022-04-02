using Perper.Extensions;

namespace PingPong.Agents;

public class Initializer
{
    private readonly AgentRegistry _agentRegistry;
    
    // ReSharper disable once MemberCanBePrivate.Global
    public const string SampleAgentsConsensus = "Local";

    public Initializer(AgentRegistry agentRegistry)
    {
        _agentRegistry = agentRegistry;
    }
    
    public async Task Init()
    {
        await _agentRegistry.ExecuteIfAgentNotFound("PlayerAgent", async () =>
        {
            var agent = await PerperContext.StartAgentAsync("PlayerAgent", SampleAgentsConsensus);
            await _agentRegistry.AddAgent(agent);
        });
    }
}