using System;
using Raven.Abstractions.Data;
using RavenMigrations.Extensions;

namespace RavenMigrations.Migrations
{
    public abstract class IndexPatchMigration : Migration
    {
        protected IndexPatchMigration()
        {
            IndexingTimeout = TimeSpan.FromMinutes(5); 
        }

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

        protected TimeSpan IndexingTimeout { get; set; }

        protected abstract string Query { get; }

        public override void Up()
        {
            DocumentStore.WaitForIndexingOf(IndexName, IndexingTimeout);
            DocumentStore.DatabaseCommands.UpdateByIndex(IndexName,
                IndexQuery,
                new ScriptedPatchRequest
                {
                    Script = UpPatch
                })
                // by waiting for completion any error that ocurrs while the docs are being patched
                // gets propagated up.
                .WaitForCompletion();
        }

        public override void Down()
        {
            if (string.IsNullOrWhiteSpace(DownPatch)) return;

            DocumentStore.WaitForIndexingOf(IndexName, IndexingTimeout);
            DocumentStore.DatabaseCommands.UpdateByIndex(IndexName,
                IndexQuery,
                new ScriptedPatchRequest
                {
                    Script = DownPatch
                })
                .WaitForCompletion();
        }
    }
}