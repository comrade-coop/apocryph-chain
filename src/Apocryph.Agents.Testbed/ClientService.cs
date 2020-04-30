using Apocryph.Agents.Testbed.Api;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace Apocryph.Agents.Testbed
{
    public class ClientService
    {
        public HashSet<WebSocket> WebSocketClients { get; set; }
        public IDictionary<string, object> AgentStates { get; set; }

        public IAsyncCollector<AgentCommands> Output { get; set; }

        public ClientService()
        {
            AgentStates = new Dictionary<string, object>();
            WebSocketClients = new HashSet<WebSocket>();
        }
    }
}
