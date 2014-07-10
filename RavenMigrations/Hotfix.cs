using Raven.Client;
using RavenMigrations.Extensions;
using RavenMigrations.Verbs;

namespace RavenMigrations
{
    public abstract class Hotfix
    {
        public virtual void Setup(IDocumentStore documentStore)
        {
            DocumentStore = documentStore;
            Alter = new Alter(documentStore);
        }

        public abstract void Apply();

        protected void WaitForIndexing()
        {
            DocumentStore.WaitForIndexing();
        }

        protected Alter Alter { get; private set; }
        protected IDocumentStore DocumentStore { get; private set; }
    }
}