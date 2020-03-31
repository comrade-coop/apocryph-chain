using Apocryph.FunctionApp.Agent;
using System;
using Wetonomy.TokenActionAgents.Messages;
using Wetonomy.TokenActionAgents.Messages.Notifications;
using Wetonomy.TokenActionAgents.Publications;
using Wetonomy.TokenActionAgents.State;
using Wetonomy.TokenManager.Messages;

namespace Wetonomy.TokenActionAgents
{
    class TokenMinterAgent<T> where T: IEquatable<T>
    {
        public static void Run(IAgentContext<RecipientState<T>> context, string sender, object message)
        {

            if (message is AbstractTriggerer msg && context.State.TriggererToAction.ContainsKey((sender, message.GetType())))
            {
                var result = RecipientState<T>.TriggerCheck(context.State, sender, msg);

                foreach (var action in result)
                {
                    if(action is MintTokenMessage<T> mintMsg)
                    {
                        context.SendMessage(context.State.TokenManagerAgent, action, null);
                    }
                    //Publication
                    //if(action is TokensMintedTriggerer<T> trigger)
                    //{
                    //    context.SendMessage(context.State.TokenManagerAgent, action, null);
                    //}
                }

                return;
            }

            switch (message)
            {
                case TokenActionAgentInitMessage organizationInitMessage:
                    context.State.TokenManagerAgent = organizationInitMessage.TokenManagerAgentCapability;
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
