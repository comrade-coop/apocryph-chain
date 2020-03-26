using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wetonomy.TokenManager.Messages
{
    class BurnTokenMessage
    {
        public BigInteger Amount { get; }
        public string From { get; }

        public BurnTokenMessage(BigInteger amount, string from)
        {
            Amount = amount;
            From = from;
        }
    }
}
