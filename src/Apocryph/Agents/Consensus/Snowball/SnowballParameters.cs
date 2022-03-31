namespace Apocryph.Agents.Consensus.Snowball
{
    public class SnowballParameters
    {
        /// <summary>
        /// Peer sample size
        /// </summary>
        public int K { get; set; }

        /// <summary>
        /// Sample threshold
        /// </summary>
        public double Alpha { get; set; }

        /// <summary>
        ///  Required confidence
        /// </summary>
        public int Beta { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="k">Peer sample size</param>
        /// <param name="alpha">Sample threshold</param>
        /// <param name="beta">Required confidence</param>
        public SnowballParameters(int k, double alpha, int beta)
        {
            K = k;
            Alpha = alpha;
            Beta = beta;
        }
    }
}