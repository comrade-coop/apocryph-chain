using Apocryph.FunctionApp.Agent;
using System;
using Wetonomy.TokenActionAgents;
using Wetonomy.TokenActionAgents.Messages;
using Wetonomy.TokenManager;
using Wetonomy.TokenManager.Messages;

namespace Wetonomy
{
	public class Program
	{
        public class DistributeTokenManagerCapabilitiesMessage
        {
            public AgentCapability TokenManagerCapability { get; set; }
        }

        public class OrganizationAgent
        {
            public static AgentContext<object> Run(object state, AgentCapability sender, object message)
            {
                var context = new AgentContext<object>(null);

                switch (message)
                {
                    case InitMessage _:
                        var cashTokenManagerInitMessage = new TokenManagerInitMessage
                        {
                            OrganizationAgentCapability = context.IssueCapability(new[]
                            {
                                nameof(DistributeTokenManagerCapabilitiesMessage)
                            })
                        };
                        context.CreateAgent("cashTokenManager", nameof(TokenManager<AgentCapability>), cashTokenManagerInitMessage,null);
                        break;
                    case DistributeTokenManagerCapabilitiesMessage distributeTokenManagerCapabilitiesMessage:
                        var tokenActionAgentInitMessage = new TokenActionAgentInitMessage
                        {
                            TokenManagerAgentCapability = distributeTokenManagerCapabilitiesMessage.TokenManagerCapability
                        };
                        context.CreateAgent("tokenSplitter", nameof(TokenSplitterAgent<AgentCapability>), tokenActionAgentInitMessage,null);

                        /*var moneyTokenManager = new TokenManager<AgentCapability>();

                        var debtTokenManager = new TokenManager<AgentCapability>();
                        var allowanceTokenManager = new TokenManager<AgentCapability>();

                        var cashTokenBurnerDebt = new TokenBurnerAgent<AgentCapability>();
                        var cashTokenBurnerAllowance = new TokenBurnerAgent<AgentCapability>();

                        var debtTokenBurner = new TokenBurnerAgent<AgentCapability>();
                        var allowanceTokenBurner = new TokenBurnerAgent<AgentCapability>();

                        var moneyTokenMinter = new TokenMinterAgent<AgentCapability>();*/
                        break;

                }
                return context;
            }
        }
		static void Main(string[] args)
		{
            Console.WriteLine("Hello World!");
		}
	}
}
