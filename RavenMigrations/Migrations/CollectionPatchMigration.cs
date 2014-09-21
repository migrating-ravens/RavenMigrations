using Raven.Client.Indexes;

namespace RavenMigrations.Migrations
{
    public abstract class IndexPatchMigration<TIndex> : IndexPatchMigration 
        where TIndex : AbstractIndexCreationTask, new()
    {
        protected override string IndexName
        {
            get { return new TIndex().IndexName; }
        }
    }

    public abstract class CollectionPatchMigration<T> : IndexPatchMigration<RavenDocumentsByEntityName>
    {
        protected override string Query
        {
            get { return "Tag:" + DocumentStore.Conventions.GetTypeTagName(typeof (T)); }
        }
    }
}