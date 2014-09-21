using Raven.Abstractions.Data;
using RavenMigrations.Extensions;

namespace RavenMigrations.Migrations
{
    public abstract class IndexPatchMigration : Migration
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
}