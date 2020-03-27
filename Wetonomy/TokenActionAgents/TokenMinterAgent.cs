using Apocryph.FunctionApp.Agent;
using System;
using System.Collections.Generic;
using System.Numerics;
using Wetonomy.State.TokenActionAgents;
using Wetonomy.TokenActionAgents.Messages;
using Wetonomy.TokenActionAgents.Publications;
using Wetonomy.TokenActionAgents.Strategies;

namespace Wetonomy.TokenActionAgents
{
    class TokenMinterAgent<T> where T: IEquatable<T>
    {
        public class TokenMinterState: RecipientState<T>
        {
            public IMintTokensStrategy<T> MintStrategy;

            public IList<object> Mint(BigInteger amount)
            {
                return MintStrategy.Mint(Recipients, amount, TokenManager);
            }
        }

        public static void Run(IAgentContext<TokenMinterState> context, string sender, object message)
        {
            switch (message)
            {
                case AbstractTriggerer triggerer:
                    Type triggererType = triggerer.GetType();
                    if (triggererType.IsAssignableFrom(context.State.Triggerer))
                    {
                        IList<object> result = context.State.Mint(triggerer.Amount);
                        foreach (var action in result)
                        {
                            context.SendMessage(null, action, null);
                        }
                    }
                    break;

                case AddRecipientMessage<T> addMessage:
                    if (context.State.AddRecipient(addMessage.Recipient))
                    {
                        context.MakePublication(new RecipientAddedPublication<T>(addMessage.Recipient));
                    }
                    break;

                case RemoveRecipientMessage<T> removeMessage:
                    if (context.State.RemoveRecipient(removeMessage.Recipient))
                    {
                        context.MakePublication(new RecipientRemovedPublication<T>(removeMessage.Recipient));
                    }
                    break;
            }
        }
    }
}
