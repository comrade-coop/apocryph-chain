using System;
using System.Collections.Generic;
using System.Text;

namespace Apocryph.Agents.Testbed.Agent
{
    public class ActionWrapper
    {
        public string MessageType { get; set; }
        public object Message { get; set; }

        public ActionWrapper(string type, object message)
        {
            MessageType = type;
            Message = message;
        }
    }
}
