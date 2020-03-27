using System;
using System.Collections.Generic;
using System.Text;

namespace Wetonomy.State.TokenActionAgents
{
    public abstract class RecipientState<T> where T: IEquatable<T>
    {
        public string TokenManager;

        public Type Triggerer;

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
    }
}
