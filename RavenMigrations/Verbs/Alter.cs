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

        /// <summary>
        ///     Allows migration of a collection of documents one document at a time.
        /// </summary>
        /// <param name="tag">The name of the collection.</param>
        /// <param name="action">The action to migrate a single document and metadata.</param>
        /// <param name="pageSize">The page size for batching the documents.</param>
        public void Collection(string tag, Action<RavenJObject, RavenJObject> action, int pageSize = 128)
        {
            var count = 0;
            do
            {
                var queryResult = DocumentStore.DatabaseCommands.Query("Raven/DocumentsByEntityName",
                    new IndexQuery
                    {
                        Query = "Tag:" + tag,
                        PageSize = pageSize,
                        Start = count,
                        SortedFields = new[] { new SortedField("LastModified") { Descending = true }, }
                        //Unless we specify a sort order, the documents will come back in arbitrary order
                        //which may change between batches.  Need to use LastModified descending rather than
                        //ascending to ensure we don't start getting docs that have just been changed by migration
                    },
                    null);

                if (queryResult.Results.Count == 0) break;
                
                count += queryResult.Results.Count;
                var cmds = new List<ICommandData>();
                foreach (var entity in queryResult.Results)
                {
                    var metadata = entity.Value<RavenJObject>("@metadata");

                    action(entity, metadata);

                    cmds.Add(new PutCommandData
                    {
                        Document = entity,
                        Metadata = metadata,
                        Key = metadata.Value<string>("@id"),
                    });
                }

                DocumentStore.DatabaseCommands.Batch(cmds.ToArray());
            } while (true);
        }

        protected IDocumentStore DocumentStore { get; private set; }
    }
}