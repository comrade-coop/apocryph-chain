using Apocryph.Ipfs;
using Apocryph.Ipfs.Fake;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;

namespace Apocryph.Agents;

public abstract class DependencyAgent
{
    // ReSharper disable once InconsistentNaming
    protected static ILogger _logger;
    // ReSharper disable once InconsistentNaming
    protected static IHashResolver _hashResolver = new FakeHashResolver();
    // ReSharper disable once InconsistentNaming
    protected static IPeerConnector _peerConnector;
    
    static DependencyAgent()
    {
        var loggerFactory = new LoggerFactory();
        loggerFactory.AddProvider(new DebugLoggerProvider());
        _logger = loggerFactory.CreateLogger("Agent");

        var peepProvider = new FakePeerConnectorProvider();
        _peerConnector = peepProvider.GetPeerConnector();
    }
}