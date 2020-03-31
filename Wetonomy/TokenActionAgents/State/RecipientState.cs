using Apocryph.FunctionApp.Agent;
using System;
using System.Collections.Generic;
using System.Text;
using Wetonomy.TokenActionAgents.Messages;

namespace Wetonomy.TokenActionAgents.State
{
    public class RecipientState<T> where T: IEquatable<T>
    {
        //Capability
        public AgentCapability TokenManagerAgent;

        public Dictionary<(string, Type), Func<RecipientState<T>, AbstractTriggerer, IList<object>>> TriggererToAction;

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


        public static IList<object> TriggerCheck(RecipientState<T> state, string sender, AbstractTriggerer message)
        {
            Func<RecipientState<T>, AbstractTriggerer, IList<object>> func = state.TriggererToAction[(sender, message.GetType())];

            IList<object> result = func.Invoke(state, message);

            return result;

        }
    }
}
