using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.CompareExchange;

namespace Raven.Migrations
{
    public class SingleInstanceRunner : MigrationRunner
    {
        private readonly ILogger<MigrationRunner> _logger;
        private readonly MigrationOptions _options;
        private readonly IDocumentStore _store;

        public SingleInstanceRunner(IDocumentStore store, MigrationOptions options, ILogger<MigrationRunner> logger)
            : base(store, options, logger)
        {
            _store = store;
            _options = options;
            _logger = logger;
        }

        public new void Run()
        {
            long? lockAcquired = null;
            using var session = _store.OpenSession();
            try
            {
                var result = _store.Operations.Send(
                    new PutCompareExchangeValueOperation<string>("LockMigrations", "locked", 0));

                if (result.Successful == false)
                {
                    _logger.LogWarning(
                        "Could not acquire lock ... already running migration or got cancelled without proper shutdown");
                    return;
                }

                lockAcquired = result.Index;
                _logger.LogInformation("acquired migration lock");
                base.Run();
            }
            finally
            {
                if (lockAcquired is {})
                {
                    var unlocked = _store.Operations.Send(
                        new DeleteCompareExchangeValueOperation<string>("LockMigrations", lockAcquired.Value));
                    if (unlocked.Successful == false)
                    {
                        _logger.LogError("Could not release lock, this need manual intervention");
                    }
                    else
                    {
                        _logger.LogInformation("released migration lock");
                    }
                }
            }
        }
    }
}