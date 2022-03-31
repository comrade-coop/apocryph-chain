using System.Collections.Generic;
using Perper.Model;

namespace Apocryph.Testing;

public class PerperAgentComparer : IEqualityComparer<PerperAgent>
{
    public bool Equals(PerperAgent x, PerperAgent y) => x.Agent == y.Agent && x.Instance == y.Instance;
    public int GetHashCode(PerperAgent obj) => obj.GetHashCode();
}