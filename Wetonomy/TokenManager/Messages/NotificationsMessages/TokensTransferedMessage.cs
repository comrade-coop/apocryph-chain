using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wetonomy.TokenManager.Messages.NotificationsMessages
{
    public class TokensTransferedMessage<T>
    {
        public BigInteger Amount { get; }
        public T From { get; }
        public T To { get; }

        public TokensTransferedMessage(BigInteger amount, T from, T to)
        {
            Amount = amount;
            From = from;
            To = to;
        }
    }
}
