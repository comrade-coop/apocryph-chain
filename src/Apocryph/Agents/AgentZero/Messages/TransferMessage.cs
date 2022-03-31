using System.Numerics;

namespace Apocryph.Agents.AgentZero.Messages
{
    public class TransferMessage
    {
        public Guid To { get; set; }
        public BigInteger Amount { get; set; }

        public TransferMessage(Guid to, BigInteger amount)
        {
            To = to;
            Amount = amount;
        }
    }
}