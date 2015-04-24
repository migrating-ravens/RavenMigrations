using Raven.Client;
using RavenMigrations.Extensions;
using RavenMigrations.Verbs;

namespace RavenMigrations
{
    public abstract class Migration
    {
        public virtual void Down()
        {
        }

        public virtual void Setup(IDocumentStore documentStore, ILogger logger)
        {
            DocumentStore = documentStore;
            Logger = logger;
            Alter = new Alter(documentStore, logger);
        }

        public abstract void Up();

        protected void WaitForIndexing()
        {
            DocumentStore.WaitForIndexing();
        }

        protected Alter Alter { get; private set; }
        protected IDocumentStore DocumentStore { get; private set; }
        protected ILogger Logger { get; private set; }
    }
}