using System;
using System.Linq;
using System.Threading;
using Raven.Client;

namespace RavenMigrations.Extensions
{
    public static class MigrationExtensions
    {
        /// <summary>
        /// Will wait until the store has completed indexing.
        /// </summary>
        /// <remarks>Taken from Matt Warren's example here http://stackoverflow.com/q/10316721/2608 </remarks>
        public static void WaitForIndexing(this IDocumentStore store)
        {
            while (store.DatabaseCommands.GetStatistics().StaleIndexes.Length != 0)
            {
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Will wait until the store has completed indexing a particular index, with an optional timeout.
        /// </summary>
        /// <remarks>Taken from Matt Warren's example here http://stackoverflow.com/q/10316721/2608 </remarks>
        public static void WaitForIndexingOf(this IDocumentStore store, string indexName, TimeSpan? timeout = null)
        {
            var startingTime = DateTime.UtcNow;
            while (store.DatabaseCommands.GetStatistics().StaleIndexes.Any(i => i == indexName))
            {
                if (timeout != null && startingTime + timeout < DateTime.UtcNow)
                    throw new InvalidOperationException(string.Format("Timeout occurred while waiting for indexing of '{0}'", indexName));
                Thread.Sleep(10);
            }
        }
    }
}