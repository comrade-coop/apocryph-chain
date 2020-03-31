using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Wetonomy.TokenActionAgents.Messages.Notifications
{
    class TokensMintedTriggerer<T>: AbstractTriggerer
    {
        public T To { get; }

        public TokensMintedTriggerer(BigInteger amount, T to)
        {
            Amount = amount;
            To = to;
        }
    }
}
