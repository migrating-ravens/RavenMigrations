using Raven.Abstractions.Data;
using Raven.Client.Indexes;
using RavenMigrations.Extensions;

namespace RavenMigrations
{
    public abstract class EntityPatchMigration<T> : Migration
    {
        public abstract string UpPatch { get; }
        public virtual string DownPatch { get { return null; } }
        public override void Up()
        {
            DocumentStore.WaitForIndexing();
            DocumentStore.DatabaseCommands.UpdateByIndex(new RavenDocumentsByEntityName().IndexName,
                new IndexQuery()
                {
                    Query = "Tag:" + DocumentStore.Conventions.GetTypeTagName(typeof (T))
                },
                new ScriptedPatchRequest
                {
                    Script = UpPatch
                });
        }

        public override void Down()
        {
            if (string.IsNullOrWhiteSpace(DownPatch)) return;

            DocumentStore.WaitForIndexing();
            DocumentStore.DatabaseCommands.UpdateByIndex(new RavenDocumentsByEntityName().IndexName,
                new IndexQuery()
                {
                    Query = "Tag:" + DocumentStore.Conventions.GetTypeTagName(typeof(T))
                },
                new ScriptedPatchRequest
                {
                    Script = DownPatch
                });
        }
    }
}