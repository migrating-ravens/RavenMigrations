using System;
using System.Collections.Generic;
using Raven.Abstractions.Commands;
using Raven.Abstractions.Data;
using Raven.Abstractions.Indexing;
using Raven.Client;
using Raven.Client.Indexes;
using Raven.Json.Linq;
using RavenMigrations.Extensions;

namespace RavenMigrations.Verbs
{
    public delegate bool DocumentMigrator(RavenJObject doc, RavenJObject metadata);

    public class Alter
    {
        public Alter(IDocumentStore documentStore)
        {
            DocumentStore = documentStore;
        }

        /// <summary>
        /// Allows migration of documents that are returned by a query to an index you can temporarily
        /// create in order to find documents quickly.
        /// </summary>
        /// <param name="map">The Map part of the index (see <see cref="IndexDefinition.Map"/>)</param>
        /// <param name="query">The lucene query text to query the index with</param>
        /// <param name="documentMigrator">
        /// The function to migrate a single document and metadata. 
        /// Returns true if the document has been modified and should be saved, false otherwise
        /// </param>
        /// <param name="pageSize">The page size for batching the documents.</param>
        public void DocumentsViaTempIndex(string map, string query, DocumentMigrator documentMigrator, int pageSize = 128)
        {
            DocumentStore.ExecuteIndex(new TemporaryMigrationIndex(map));
            DocumentStore.WaitForIndexing();

            try
            {
                QueryHeaderInformation headerInfo;
                var enumerator = DocumentStore.DatabaseCommands.StreamQuery(TemporaryMigrationIndex.Name,
                    new IndexQuery
                    {
                        Query = query,
                    },
                    out headerInfo);

                MigrateDocumentsFromEnumerator(enumerator, documentMigrator, pageSize);
            }
            finally
            {
                DocumentStore.DatabaseCommands.DeleteIndex(TemporaryMigrationIndex.Name);
            }
        }

        /// <summary>
        ///     Allows migration of a collection of documents one document at a time, with the ability
        ///     to skip modifying documents when they don't need it.
        /// </summary>
        /// <param name="tag">The name of the collection.</param>
        /// <param name="documentMigrator">
        /// The function to migrate a single document and metadata. 
        /// Returns true if the document has been modified and should be saved, false otherwise
        /// </param>
        /// <param name="pageSize">The page size for batching the documents.</param>
        public void CollectionSubset(string tag, DocumentMigrator documentMigrator, int pageSize = 128)
        {
            QueryHeaderInformation headerInfo;
            var enumerator = DocumentStore.DatabaseCommands.StreamQuery("Raven/DocumentsByEntityName",
                new IndexQuery
                {
                    Query = "Tag:" + tag,
                },
                out headerInfo);


            MigrateDocumentsFromEnumerator(enumerator, documentMigrator, pageSize);
        }

        private void MigrateDocumentsFromEnumerator(IEnumerator<RavenJObject> enumerator, DocumentMigrator documentMigrator, int pageSize)
        {
            var cmds = new List<ICommandData>();
            using (enumerator)
            while (enumerator.MoveNext())
            {
                var entity = enumerator.Current;
                var metadata = entity.Value<RavenJObject>("@metadata");

                if (documentMigrator(entity, metadata) == false)
                    continue;

                cmds.Add(new PutCommandData
                {
                    Document = entity,
                    Metadata = metadata,
                    Key = metadata.Value<string>("@id"),
                });

                if (cmds.Count == pageSize)
                {
                    DocumentStore.DatabaseCommands.Batch(cmds.ToArray());
                    cmds.Clear();
                }
            }

            if (cmds.Count > 0)
                DocumentStore.DatabaseCommands.Batch(cmds.ToArray());
        }

        /// <summary>
        ///     Allows migration of a collection of documents one document at a time.
        /// </summary>
        /// <param name="tag">The name of the collection.</param>
        /// <param name="action">The action to migrate a single document and metadata.</param>
        /// <param name="pageSize">The page size for batching the documents.</param>
        public void Collection(string tag, Action<RavenJObject, RavenJObject> action, int pageSize = 128)
        {
            CollectionSubset(tag, (doc, meta) =>
            {
                action(doc, meta);
                return true;
            }, pageSize);
        }

        protected IDocumentStore DocumentStore { get; private set; }

        internal class TemporaryMigrationIndex : AbstractIndexCreationTask
        {
            public const string Name = "TemporaryMigrationIndex";
            private readonly string _map;

            public TemporaryMigrationIndex(string map)
            {
                _map = map;
            }

            public override IndexDefinition CreateIndexDefinition()
            {
                return new IndexDefinition
                {
                    Name = Name,
                    Map = _map
                };
            }
        }
    }
}