using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Json.Linq;
using RavenMigrations.Extensions;

namespace RavenMigrations.Verbs
{
    public class Metadata
    {
        public Metadata(IDocumentStore documentStore)
        {
            DocumentStore = documentStore;
        }

        /// <summary>
        /// Changes the Raven-Entity-Name tag on the metadata to rename the collection.
        /// </summary>
        public void CollectionName(string tag, string newCollectionName)
        {
            UpdateMetadataCore(tag, Constants.RavenEntityName, newCollectionName);
        }

        /// <summary>
        /// Changes the Raven-Clr-Type tag on the metadata to allow loading by a new CLR class.
        /// </summary>
        public void RavenClrType(string tag, string newRavenClrType)
        {
            UpdateMetadataCore(tag, Constants.RavenClrType, newRavenClrType);
        }

        private void UpdateMetadataCore(string tag, string attributeName, string newValue)
        {
            DocumentStore.WaitForIndexing();
            DocumentStore.DatabaseCommands.UpdateByIndex(
                "Raven/DocumentsByEntityName",
                new IndexQuery {Query = "Tag:" + tag},
                new[]
                {
                    new PatchRequest
                    {
                        Type = PatchCommandType.Modify,
                        Name = "@metadata",
                        Nested = new[]
                        {
                            new PatchRequest
                            {
                                Type = PatchCommandType.Set,
                                Name = attributeName,
                                Value = new RavenJValue(newValue)
                            }
                        }
                    }
                });
        }

        protected IDocumentStore DocumentStore { get; private set; }
    }
}