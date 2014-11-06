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
        /// Will wait until the store has completed indexing.
        /// </summary>
        /// <remarks>Taken from Matt Warren's example here http://stackoverflow.com/q/10316721/2608 </remarks>
        public static void WaitForIndexingOf(this IDocumentStore store, string indexName)
        {
            while (store.DatabaseCommands.GetStatistics().StaleIndexes.Any(i => i == indexName))
            {
                Thread.Sleep(10);
            }
        }
    }
}