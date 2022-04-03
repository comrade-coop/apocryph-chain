using Apocryph.Agents.Consensus.Local;
using Apocryph.Agents.Consensus.Snowball;
using Apocryph.Agents.Executor;
using Apocryph.Agents.Koth;
using Apocryph.Agents.Router;
using Serilog;
using Perper.Application;
using PingPong.Agents;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(_ =>
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
    })
    .ConfigureServices(services =>
    {
        services.AddTransient<IExecutorAdapter>();
        services.AddTransient<ILocalConsensusAdapter>();
        services.AddTransient<IKoTHAdapter>();
        services.AddTransient<IRoutingAdapter>();
    })
    .ConfigurePerper(builder =>
    {
        builder
            .AddClassHandlers<Executor>()
            .AddClassHandlers<KoTH>()
            .AddClassHandlers<KoTHSimpleMiner>()
            .AddClassHandlers<LocalConsensus>()
            .AddClassHandlers<SnowballConsensus>()
            .AddClassHandlers<Routing>()
            .AddClassHandlers<Player>()
            .AddClassHandlers<Initializer>();
    })
    .Build();

await host.RunAsync();