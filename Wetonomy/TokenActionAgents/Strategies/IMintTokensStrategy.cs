using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wetonomy.TokenActionAgents.Strategies
{
    interface IMintTokensStrategy<T>
    {
        public IList<object> Mint(IEnumerable<T> recipients, BigInteger amount, string tokenManager);
    }
}
