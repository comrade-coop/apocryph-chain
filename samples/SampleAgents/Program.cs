using Apocryph.Agents.Consensus.Local;
using Apocryph.Agents.Consensus.Snowball;
using Apocryph.Agents.Executor;
using Apocryph.Agents.Koth;
using Apocryph.Agents.Router;
using Perper.Application;
using Perper.Extensions;
using SampleAgents.Agents;
using Serilog;

const string SAMPLE_AGENTS_CONSENSUS = "Local";

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

static async Task DeployAgent()
{
    if (await PerperState.GetOrDefaultAsync("AGENT_DEPLOYED", false)) return;
    
    await PerperContext.StartAgentAsync("PingPong", SAMPLE_AGENTS_CONSENSUS);
    await PerperState.SetAsync("AGENT_DEPLOYED", true);
}

await new PerperStartup()
    .AddClassHandlers<Executor>()
    .AddClassHandlers<KoTH>()
    .AddClassHandlers<KoTHSimpleMiner>()
    .AddClassHandlers<LocalConsensus>()
    .AddClassHandlers<SnowballConsensus>()
    .AddClassHandlers<Routing>()
    .AddClassHandlers<PingPong>()
    .AddInitHandler("", DeployAgent)
    .RunAsync(default)
    .ConfigureAwait(false);