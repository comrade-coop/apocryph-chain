using Apocryph.FunctionApp.Agent;
using System.Collections.Generic;
using System.Numerics;
using Wetonomy.TokenManager.Messages;
using Wetonomy.TokenManager.Messages.NotificationsMessages;
using Wetonomy.TokenManager.Publications;

namespace Wetonomy.TokenManager
{
    public class TokenManager<T> where T : class
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

        public static AgentContext<TokenManagerState> Run(object state, AgentCapability sender, object message)
        {
            var context = new AgentContext<TokenManagerState>(state as TokenManagerState);
            switch (message)
            {
                case TokenManagerInitMessage tokenManagerInitMessage:
                    var distributeCapabilityMessage = new Program.DistributeTokenManagerCapabilitiesMessage
                    {
                        Id = tokenManagerInitMessage.Id,
                        TokenManagerCapabilities = new Dictionary<string, AgentCapability>() {
                            {"BurnTokenMessage", context.IssueCapability(new[]{ nameof(BurnTokenMessage<T>) }) },
                            {"MintTokenMessage", context.IssueCapability(new[]{ nameof(MintTokenMessage<T>) }) },
                            {"TransferTokenMessage", context.IssueCapability(new[]{ nameof(TransferTokenMessage<T>) }) },
                        }
                    };

                    context.SendMessage(tokenManagerInitMessage.OrganizationAgentCapability, distributeCapabilityMessage, null);
                    break;
                case BurnTokenMessage<T> burnTokenMessage:
                    if(context.State.Burn(burnTokenMessage.Amount, burnTokenMessage.From))
                    {
                        context.SendMessage(burnTokenMessage.From as AgentCapability, new TokensBurnedNotification<T>(burnTokenMessage.Amount, burnTokenMessage.From), null);

                        context.MakePublication(
                            new TokenBurnPublication<T>(burnTokenMessage.Amount, burnTokenMessage.From)
                        );
                    }
                    break;

                case MintTokenMessage<T> mintTokenMessage:

                    if (context.State.Mint(mintTokenMessage.Amount, mintTokenMessage.To))
                    {
                        context.SendMessage(mintTokenMessage.To as AgentCapability, new TokensMintedNotification<T>(mintTokenMessage.Amount, mintTokenMessage.To), null);

                        context.MakePublication(
                            new TokenMintPublication<T>(mintTokenMessage.Amount, mintTokenMessage.To)
                        );
                    }
                    break;

                case TransferTokenMessage<T> transferTokenMessage:
                    if (context.State.Transfer(transferTokenMessage.Amount, transferTokenMessage.From, transferTokenMessage.To))
                    {

                        context.SendMessage(transferTokenMessage.From as AgentCapability, new TokensTransferedNotification<T>(transferTokenMessage.Amount, transferTokenMessage.From, transferTokenMessage.To), null);
                        context.SendMessage(transferTokenMessage.To as AgentCapability, new TokensTransferedNotification<T>(transferTokenMessage.Amount, transferTokenMessage.From, transferTokenMessage.To), null);

                        context.MakePublication(
                            new TokenTransferPublication<T>(transferTokenMessage.Amount, transferTokenMessage.From, transferTokenMessage.To)
                        );
                    }
                    break;
            }
            return context;
        }
    }
}
