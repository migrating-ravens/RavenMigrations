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
            QueryHeaderInformation headerInfo;
            var enumerator = DocumentStore.DatabaseCommands.StreamQuery("Raven/DocumentsByEntityName",
                new IndexQuery
                {
                    Query = "Tag:" + tag,
                },
                out headerInfo);


            var cmds = new List<ICommandData>();
            using (enumerator)
            while (enumerator.MoveNext())
            {
                var entity = enumerator.Current;
                var metadata = entity.Value<RavenJObject>("@metadata");

                action(entity, metadata);

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

        protected IDocumentStore DocumentStore { get; private set; }
    }
}