using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Apocryph.FunctionApp.Model;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace Apocryph.FunctionApp
{
    public static class ValidatorLauncher
    {
        [FunctionName("ValidatorLauncher")]
        public static async Task Run([PerperStreamTrigger("ValidatorLauncher")] IPerperStreamContext context,
            [PerperStreamTrigger("agentId")] string agentId,
            [PerperStreamTrigger("validatorSet")] ValidatorSet validatorSet,
            [PerperStreamTrigger("ipfsGateway")] string ipfsGateway,
            [PerperStreamTrigger("privateKey")] string privateKey,
            [PerperStreamTrigger("self")] ValidatorKey self)
        {
            var topic = "apocryph-agent-" + agentId;

            await using var ipfsStream = await context.StreamFunctionAsync("IpfsInput", new
            {
                ipfsGateway,
                topic
            });

            var commitsStream = ipfsStream;
            var votesStream = ipfsStream;
            var proposalsStream = ipfsStream;

            // Proposer (Proposing)

            await using var currentProposerStream = await context.StreamFunctionAsync("CurrentProposer", new
            {
                commitsStream,
                validatorSet
            });

            await using var _committerStream = await context.StreamFunctionAsync("Committer", new
            {
                commitsStream,
                validatorSet
            });

            await using var committerStream = await context.StreamFunctionAsync("IpfsLoader", new
            {
                ipfsGateway,
                hashStream = _committerStream
            });

            await using var proposerRuntimeStream = await context.StreamFunctionAsync("ProposerRuntime", new
            {
                self,
                currentProposerStream,
                committerStream
            });

            await using var proposerStream = await context.StreamFunctionAsync("Proposer", new
            {
                commitsStream,
                proposerRuntimeStream
            });

            // Validator (Voting)

            await using var _validatorStream = await context.StreamFunctionAsync("Validator", new
            {
                committerStream,
                currentProposerStream,
                proposalsStream,
                validatorSet
            });

            await using var validatorStream = await context.StreamFunctionAsync("IpfsLoader", new
            {
                ipfsGateway,
                hashStream = _validatorStream
            });

            await using var _validatorRuntimeStream = await context.StreamFunctionAsync("ProposerRuntime", new
            {
                validatorStream,
                committerStream
            });

            await using var validatorRuntimeStream = await context.StreamFunctionAsync("IpfsSaver", new
            {
                ipfsGateway,
                dataStream = _validatorRuntimeStream
            });

            await using var votingStream = await context.StreamFunctionAsync("Voting", new
            {
                runtimeStream = validatorRuntimeStream,
                proposalsStream
            });

            // Consensus (Committing)

            await using var consensusStream = await context.StreamFunctionAsync("Consensus", new
            {
                votesStream
            });

            foreach (var stream in new[] {proposerStream, votingStream, consensusStream})
            {
                await using var saverStream = await context.StreamFunctionAsync("IpfsSaver", new
                {
                    ipfsGateway,
                    dataStream = stream
                });

                await using var signerStream = await context.StreamFunctionAsync("Signer", new
                {
                    self,
                    privateKey,
                    dataStream = saverStream
                });

                await context.StreamActionAsync("IpfsOutput", new
                {
                    ipfsGateway,
                    topic,
                    dataStream = signerStream
                });
            }
        }
    }
}