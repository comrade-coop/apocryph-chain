using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wetonomy.TokenManager.Messages.NotificationsMessages
{
    public class TokensBurnedMessage<T>
    {
        public BigInteger Amount { get; }
        public T From { get; }

        public TokensBurnedMessage(BigInteger amount, T from)
        {
            Amount = amount;
            From = from;
        }
    }
}
