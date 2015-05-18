using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Abstractions.Commands;
using Raven.Abstractions.Data;
using Raven.Abstractions.Extensions;
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
        public void Collection(string tag, Func<RavenJObject, RavenJObject, IEnumerable<ICommandData>> action, int pageSize = 128)
        {
            QueryHeaderInformation headerInfo;
            var enumerator = DocumentStore.DatabaseCommands.StreamQuery("Raven/DocumentsByEntityName",
                new IndexQuery
                {
                    Query = "Tag:" + tag,
                },
                out headerInfo);


            var actions = new RavenActions();
            using (enumerator)
            while (enumerator.MoveNext())
            {
                var entity = enumerator.Current;
                var metadata = entity.Value<RavenJObject>("@metadata");

                var actionCommands = action(entity, metadata);

                if (actionCommands != null && actionCommands.ToList().Any())
                {
                    actions.AdditionalMigrationCommands.AddRange(actionCommands);
                }

                actions.MigrationCommands.Add(new PutCommandData
                {
                    Document = entity,
                    Metadata = metadata,
                    Key = metadata.Value<string>("@id"),
                });

                if (actions.MigrationCommands.Count == pageSize)
                {
                    DocumentStore.DatabaseCommands.Batch(actions.AllCommands());
                    actions.ClearMigrationCommands();
                }
            }

            if (actions.AllCommands().Count > 0)
                DocumentStore.DatabaseCommands.Batch(actions.AllCommands());
        }

        protected IDocumentStore DocumentStore { get; private set; }
    }

    public class RavenActions
    {
        public IList<ICommandData> MigrationCommands { get; set; }
        public IList<ICommandData> AdditionalMigrationCommands { get; set; }

        public RavenActions()
        {
            MigrationCommands = new List<ICommandData>();
            AdditionalMigrationCommands = new List<ICommandData>();
        }

        public IList<ICommandData> AllCommands()
        {
            return MigrationCommands.Union(AdditionalMigrationCommands).ToList();
        }

        public void ClearMigrationCommands()
        {
            MigrationCommands.Clear();
            AdditionalMigrationCommands.Clear();
        }
    }
}