using Apocryph.FunctionApp.Agent;
using System;
using System.Collections.Generic;
using System.Numerics;
using Wetonomy.State.TokenActionAgents;
using Wetonomy.TokenActionAgents.Messages;
using Wetonomy.TokenActionAgents.Publications;
using Wetonomy.TokenManager.Messages;
using Wetonomy.TokenManager.Messages.NotificationsMessages;
using System.Linq;

namespace Wetonomy.TokenActionAgents
{
    class TokenBurnerAgent<T> where T : IEquatable<T>
    {
        public class TokenBurnerState: RecipientState<T>
        {
            public HashSet<TokensTransferedMessage<T>> TransferMessages = new HashSet<TokensTransferedMessage<T>>();

            public bool GetTokens(T from, BigInteger amount, out T sender)
            {
                sender = default;
                TokensTransferedMessage<T> element = TransferMessages.FirstOrDefault(x => x.From.Equals(from) && x.Amount == amount);
                if (element == null) return false;
                BigInteger current = element.Amount;
                if (current > amount)
                {
                    //Not sure if we need this scenario
                    var newTokensMsg = new TokensTransferedMessage<T>(element.Amount, element.From, element.To);
                    sender = element.To;
                    TransferMessages.Add(newTokensMsg);
                    TransferMessages.Remove(element);
                    return true;
                }
                if (current == amount)
                {
                    sender = element.To;
                    TransferMessages.Remove(element);
                    return true;
                }
                return false;
            }
        }

        public static void Run(IAgentContext<TokenBurnerState> context, string sender, object message)
        {
            if (message is AbstractTriggerer msg && context.State.TriggererToAction.ContainsKey((sender, message.GetType())))
            {
                var result = RecipientState<T>.TriggerCheck(context, sender, msg);

                foreach (BurnTokenMessage<T> action in result)
                {
                    context.SendMessage(null, action, null);
                }

                return;
            }

            switch(message)
            {
                case TokensTransferedMessage<T> transferedMessage:
                    if (context.State.AddRecipient(transferedMessage.From))
                    {
                        context.State.TransferMessages.Add(transferedMessage);
                        context.MakePublication(new RecipientAddedPublication<T>(transferedMessage.From));
                    }
                    break;

                case GetTokensMessage<T> getTokensMessage:
                    T agentSender;
                    if (context.State.GetTokens(getTokensMessage.Recipient, getTokensMessage.Amount, out agentSender))
                    {
                        var transfer = new TransferTokenMessage<T>(getTokensMessage.Amount, agentSender, getTokensMessage.Recipient);
                        context.SendMessage(null, transfer, null);
                    }
                    break;
            }
        }

    }
}
