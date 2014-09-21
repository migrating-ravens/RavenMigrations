using Raven.Abstractions.Data;
using Raven.Client.Indexes;
using RavenMigrations.Extensions;

namespace RavenMigrations
{
    public abstract class PatchMigration : Migration
    {
        public abstract string UpPatch { get; }
        public virtual string DownPatch { get { return null; } }

        protected abstract string IndexName { get; }

        protected virtual IndexQuery IndexQuery
        {
            get
            {
                return new IndexQuery {Query = Query};
            }
        }

        protected abstract string Query { get; }

        public override void Up()
        {
            DocumentStore.WaitForIndexing();
            DocumentStore.DatabaseCommands.UpdateByIndex(IndexName,
                IndexQuery,
                new ScriptedPatchRequest
                {
                    Script = UpPatch
                });
        }

        public override void Down()
        {
            if (string.IsNullOrWhiteSpace(DownPatch)) return;

            DocumentStore.WaitForIndexing();
            DocumentStore.DatabaseCommands.UpdateByIndex(IndexName,
                IndexQuery,
                new ScriptedPatchRequest
                {
                    Script = DownPatch
                });
        }
    }

    public abstract class PatchMigration<TIndex> : PatchMigration 
        where TIndex : AbstractIndexCreationTask, new()
    {
        protected override string IndexName
        {
            get { return new TIndex().IndexName; }
        }
    }

    public abstract class CollectionPatchMigration<T> : PatchMigration<RavenDocumentsByEntityName>
    {
        protected override string Query
        {
            get { return "Tag:" + DocumentStore.Conventions.GetTypeTagName(typeof (T)); }
        }
    }
}