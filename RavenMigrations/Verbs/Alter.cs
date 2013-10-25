using System;
using System.Collections.Generic;
using Raven.Abstractions.Commands;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Json.Linq;

namespace RavenMigrations.Verbs
{
    public class Alter
    {
        public Alter(IDocumentStore documentStore)
        {
            DocumentStore = documentStore;
        }

        protected IDocumentStore DocumentStore { get; private set; }

        /// <summary>
        /// Allows migration of a collection of documents one document at a time.
        /// </summary>
        /// <param name="tag">The name of the collection.</param>
        /// <param name="action">The action to migrate a single document.</param>
        /// <param name="pageSize">The page size for batching the documents.</param>
        public void Collection(string tag, Action<RavenJObject> action, int pageSize = 128)
        {
            var count = 0;
            do
            {
                var queryResult = DocumentStore.DatabaseCommands.Query("Raven/DocumentsByEntityName",
                    new IndexQuery
                    {
                        Query = "Tag:" + tag,
                        PageSize = pageSize,
                        Start = count
                    },
                    null);

                if (queryResult.Results.Count == 0) break;

                count += queryResult.Results.Count;
                var cmds = new List<ICommandData>();
                foreach (var result in queryResult.Results)
                {
                    action(result);

                    var value = result.Value<RavenJObject>("@metadata");
                    cmds.Add(new PutCommandData
                    {
                        Document = result,
                        Metadata = value,
                        Key = value.Value<string>("@id"),
                    });
                }

                DocumentStore.DatabaseCommands.Batch(cmds.ToArray());
            }
            while (true);

        }
        
    }
}