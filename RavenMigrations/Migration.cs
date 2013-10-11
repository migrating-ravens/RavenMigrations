using Raven.Client;

namespace RavenMigrations
{
    public abstract class Migration
    {
        protected IDocumentStore DocumentStore { get; private set; }

        public abstract void Up();
        public virtual void Down() {}

        public virtual void Setup(IDocumentStore documentStore)
        {
            DocumentStore = documentStore;
        }
    }
}