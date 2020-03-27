using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wetonomy.TokenManager.Messages.NotificationsMessages
{
    public class TokensMintedMessage<T>
    {
        public BigInteger Amount { get; }

        public T To { get; }

        public TokensMintedMessage(BigInteger amount, T to)
        {
            Amount = amount;
            To = to;
        }
    }
}
