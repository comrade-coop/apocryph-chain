using Apocryph.FunctionApp.Agent;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Wetonomy.State.TokenActionAgents;
using Wetonomy.TokenActionAgents.Messages;
using Wetonomy.TokenActionAgents.Publications;
using Wetonomy.TokenActionAgents.Strategies;

namespace Wetonomy.TokenActionAgents
{
    class TokenSplitterAgent<T> where T: IEquatable<T>
    {
        public class TokenSolitterState : RecipientState<T>
        {
            public ISplitTokensStrategy<T> SplitStrategy;

            public IList<object> Split(BigInteger amount)
            {
                return SplitStrategy.Split(Recipients, amount, TokenManager);
            }
        }
        public static void Run(IAgentContext<TokenSolitterState> context, string sender, object message)
        {
            switch (message)
            {
                case AbstractTriggerer triggerer:
                    Type triggererType = triggerer.GetType();
                    if (triggererType.IsAssignableFrom(context.State.Triggerer))
                    {
                        IList<object> result = context.State.Split(triggerer.Amount);
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
