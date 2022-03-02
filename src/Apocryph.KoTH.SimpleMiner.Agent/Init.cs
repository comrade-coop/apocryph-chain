using Perper.Extensions;

namespace Apocryph.KoTH.SimpleMiner.Agent;

public class Init
{
    public async Task RunAsync(object? input)
    {
        await PerperContext.Stream("SimpleMiner").StartAsync(input);
    }
}