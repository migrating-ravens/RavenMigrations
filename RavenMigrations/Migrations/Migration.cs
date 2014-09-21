using Raven.Client;
using RavenMigrations.Extensions;
using RavenMigrations.Verbs;

namespace RavenMigrations.Migrations
{
    public abstract class Migration
    {
        public virtual void Down()
        {
        }

        public virtual void Setup(IDocumentStore documentStore)
        {
            DocumentStore = documentStore;
            Alter = new Alter(documentStore);
        }

        public abstract void Up();

        protected void WaitForIndexing()
        {
            DocumentStore.WaitForIndexing();
        }

        protected Alter Alter { get; private set; }
        protected IDocumentStore DocumentStore { get; private set; }
    }
}