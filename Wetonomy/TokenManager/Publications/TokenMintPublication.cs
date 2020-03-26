using System.Collections.Generic;
using System.Numerics;
using Apocryph.FunctionApp.Model;

namespace Wetonomy.TokenManager.Publications
{
    public class TokenMintPublication
    {
        public BigInteger Amount { get; }

        public string To { get; }

        public TokenMintPublication(BigInteger amount, string to)
        {
            Amount = amount;
            To = to;
        }
    }
}