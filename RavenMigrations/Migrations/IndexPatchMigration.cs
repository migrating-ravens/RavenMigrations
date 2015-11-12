using System;
using System.Collections.Generic;
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

        public virtual Dictionary<string, object> UpPatchValues
        {
            get { return new Dictionary<string, object>(); }
        }
        public virtual string DownPatch { get { return null; } }
        public virtual Dictionary<string, object> DownPatchValues
        {
            get { return new Dictionary<string, object>(); }
        }

        protected abstract string IndexName { get; }

        protected virtual IndexQuery IndexQuery
        {
            get
            {
                return new IndexQuery {Query = Query};
            }
        }

        protected TimeSpan IndexingTimeout { get; set; }

        protected virtual BulkOperationOptions GetOperationOptions()
        {
            return new BulkOperationOptions
            {
                StaleTimeout = IndexingTimeout
            };
        }

        protected abstract string Query { get; }

        public override void Up()
        {
            DocumentStore.DatabaseCommands.UpdateByIndex(IndexName,
                IndexQuery,
                new ScriptedPatchRequest
                {
                    Script = UpPatch,
                    Values = UpPatchValues,
                },
                GetOperationOptions())
                // by waiting for completion any error that ocurrs while the docs are being patched
                // gets propagated up.
                .WaitForCompletion();
        }

        public override void Down()
        {
            if (string.IsNullOrWhiteSpace(DownPatch)) return;
            
            DocumentStore.DatabaseCommands.UpdateByIndex(IndexName,
                IndexQuery,
                new ScriptedPatchRequest
                {
                    Script = DownPatch,
                    Values = DownPatchValues,
                },
                GetOperationOptions())
                .WaitForCompletion();
        }
    }
}