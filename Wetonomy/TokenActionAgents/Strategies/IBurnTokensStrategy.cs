using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wetonomy.TokenActionAgents.Strategies
{
    interface IBurnTokensStrategy<T>
    {
        public IList<object> Burn(IEnumerable<T> recipients, BigInteger amount, string tokenManager);
    }
}
