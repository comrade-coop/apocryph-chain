using Apocryph.FunctionApp.Agent;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Wetonomy.TokenManager.Messages;

namespace Wetonomy.TokenManager
{
    public static class TokenManager<T>
    {
        public class TokenManagerState: ITokenManagerState<T>
        {
            public BigInteger TotalBalance { get; private set; }
            public Dictionary<T, BigInteger> TokenBalances = new Dictionary<T, BigInteger>();

            public bool Burn(BigInteger amount, T from)
            {
                if (!TokenBalances.ContainsKey(from)) return false;
                BigInteger current = TokenBalances[from];
                if(current > amount)
                {
                    TokenBalances[from] -= amount;
                    TotalBalance-=amount;
                    return true;
                }
                if(current == amount)
                {
                    TotalBalance -= amount;
                    TokenBalances.Remove(from);
                    return true;
                }
                return false;
            }

            public bool Mint(BigInteger amount, T to)
            {
                if (TokenBalances.ContainsKey(to)) TokenBalances[to] += amount;
                
                else TokenBalances.Add(to, amount);

                TotalBalance += amount;
                return true;
            }

            public bool Transfer(BigInteger amount, T from, T to)
            {
                if (!TokenBalances.ContainsKey(from)) return false;
                BigInteger current = TokenBalances[from];
                if (current > amount)
                {
                    TokenBalances[from] -= amount;
                    TokenBalances[to] -= amount;
                    return true;
                }
                if (current == amount)
                {
                    TokenBalances.Remove(from);
                    TokenBalances.Add(to, amount);
                    return true;
                }
                return false;
            }
        }

        public static void Run(IAgentContext<TokenManagerState> context, string sender, object message)
        {
            switch (message)
            {
                case BurnTokenMessage<T> burnTokenMessage:
                    context.State.Burn(burnTokenMessage.Amount, burnTokenMessage.From);
                    break;

                case MintTokenMessage<T> mintTokenMessage:
                    context.State.Mint(mintTokenMessage.Amount, mintTokenMessage.To);
                    break;

                case TransferTokenMessage<T> transferTokenMessage:
                    context.State.Transfer(transferTokenMessage.Amount, transferTokenMessage.From, transferTokenMessage.To);
                    break;
            }
        }
    }
}
