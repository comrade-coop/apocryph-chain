using Apocryph.FunctionApp.Agent;
using System;
using Wetonomy.TokenActionAgents;
using Wetonomy.TokenManager;
namespace Wetonomy
{
	class Program
	{
        class OrganizationAgent
        {
            public static AgentContext<object> Run(object state, AgentCapability sender, object message)
            {
                var context = new AgentContext<object>(null);

                context.CreateAgent("cashTokenManager", nameof(TokenManager<AgentCapability>));
                

                var moneyTokenManager = new TokenManager<AgentCapability>();

                var debtTokenManager = new TokenManager<AgentCapability>();
                var allowanceTokenManager = new TokenManager<AgentCapability>();

                context.CreateAgent("tokenSplitter", nameof(TokenSplitterAgent<AgentCapability>));
                //tokenSplitter.Run(null, new InitMessage(new AgentCapability(cashTokenManager), )

                var cashTokenBurnerDebt = new TokenBurnerAgent<AgentCapability>();
                var cashTokenBurnerAllowance = new TokenBurnerAgent<AgentCapability>();

                var debtTokenBurner = new TokenBurnerAgent<AgentCapability>();
                var allowanceTokenBurner = new TokenBurnerAgent<AgentCapability>();

                var moneyTokenMinter = new TokenMinterAgent<AgentCapability>();

                switch (message)
                {
                    case AgentCapability capability:
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
