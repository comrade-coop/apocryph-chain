using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace Apocryph.Agents.Testbed
{
    public class WebSocketService
    {
        public HashSet<WebSocket> WebSocketClients { get; set; }

        public WebSocketService()
        {
            WebSocketClients = new HashSet<WebSocket>();
        }
    }
}
