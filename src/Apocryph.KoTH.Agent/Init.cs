using Perper.Extensions;

namespace Apocryph.KoTH.Agent;

public class Init
{
    public async Task RunAsync(object? input)
    {
        await PerperContext.Stream("KoTHProcessor").StartAsync(input);
    }
}