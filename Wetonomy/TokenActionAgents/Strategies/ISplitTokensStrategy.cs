using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wetonomy.TokenActionAgents.Strategies
{
    interface ISplitTokensStrategy<T>
    {
        public IList<object> Split(IEnumerable<T> recipients, BigInteger amount, string tokenManager);
    }
}
