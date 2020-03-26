using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wetonomy.TokenManager.Messages
{
    class MintTokenMessage
    {
        public BigInteger Amount { get; }
        public string To { get; }

        public MintTokenMessage(BigInteger amount, string to)
        {
            Amount = amount;
            To = to;
        }
    }
}
