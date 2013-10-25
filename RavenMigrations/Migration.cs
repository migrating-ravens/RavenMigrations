using Raven.Client;
using RavenMigrations.Verbs;

namespace RavenMigrations
{
    public abstract class Migration
    {
        protected IDocumentStore DocumentStore { get; private set; }

        public abstract void Up();
        public virtual void Down() {}
        protected Alter Alter { get; private set; }

        public virtual void Setup(IDocumentStore documentStore)
        {
            DocumentStore = documentStore;
            Alter = new Alter(documentStore);
        }
    }
}