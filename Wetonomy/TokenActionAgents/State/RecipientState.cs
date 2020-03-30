using Apocryph.FunctionApp.Agent;
using System;
using System.Collections.Generic;
using System.Text;
using Wetonomy.TokenActionAgents.Messages;

namespace Wetonomy.State.TokenActionAgents
{
    public class RecipientState<T> where T: IEquatable<T>
    {
        //Capability
        public string TokenManagerAgent;

        public Dictionary<(string, Type), Func<IAgentContext<RecipientState<T>>, AbstractTriggerer, IList<object>>> TriggererToAction;

        public List<T> Recipients = new List<T>();

        public bool AddRecipient(T recipient)
        {
            Recipients.Add(recipient);

            return true;
        }

        public bool RemoveRecipient(T recipient)
        {
            int index = Recipients.FindIndex(x => x.Equals(recipient));
            if (index == -1) return false;
            Recipients.RemoveAt(index);

            return true;
        }


        public static IList<object> TriggerCheck(IAgentContext<RecipientState<T>> context, string sender, AbstractTriggerer message)
        {
            Func<IAgentContext<RecipientState<T>>, AbstractTriggerer, IList<object>> func = context.State.TriggererToAction[(sender, message.GetType())];

            IList<object> result = func.Invoke(context, message);

            return result;

        }
    }
}
