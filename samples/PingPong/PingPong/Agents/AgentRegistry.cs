using Perper.Model;
using Serilog;

namespace PingPong.Agents;

public class AgentRegistry
{
    private readonly IPerper _perper;
    private readonly PerperState _internalState = new (nameof(AgentRegistry));

    public AgentRegistry(IPerper perper)
    {
        _perper = perper;
    }

    public async Task AddAgent(PerperAgent agent)
    {
        await _perper.States.SetAsync(_internalState, agent.Agent, agent);
    }

    public async Task<PerperAgent> GetAgentInstance(string agentName)
    {
        var result = await _perper.States.TryGetAsync<PerperAgent>(_internalState, agentName);
        return (result.Exists ? result.Value : null)!;
    }

    public async Task ExecuteIfAgentNotFound(string agentName, Func<Task> action)
    {
        var result = await _perper.States.TryGetAsync<PerperAgent>(_internalState, agentName);
        if (!result.Exists)
        {
            try
            {
                await action.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to execute ExecuteIfAgentNotFound method");
            }
        }
    }
}