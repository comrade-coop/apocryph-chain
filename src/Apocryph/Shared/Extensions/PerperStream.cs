using Perper.Model;

namespace Apocryph.Shared.Extensions;

public class PerperStream<T> : Perper.Model.PerperStream
{
    public PerperStream(string stream, long startIndex = -1, long stride = 0, bool localToData = false) : base(stream,
        startIndex, stride, localToData)
    {
    }
}

public class ApocryphAgent : PerperAgent
{
    public ApocryphAgent(string agent, string instance) : base(agent, instance)
    {
    }
    
    // PUBLIC KEY
}