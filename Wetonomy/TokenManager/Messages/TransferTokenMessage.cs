using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wetonomy.TokenManager.Messages
{
    class TransferTokenMessage
    {
        public BigInteger Amount { get; }
        public string From { get; }
        public string To { get; }

        public TransferTokenMessage(BigInteger amount, string from, string to)
        {
            Amount = amount;
            From = from;
            To = to;
        }
    }
}
