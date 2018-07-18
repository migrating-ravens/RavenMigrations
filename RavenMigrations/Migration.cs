using Raven.Client.Documents;

namespace Raven.Migrations
{
    public abstract class Migration
    {
        public virtual void Down()
        {
        }

        public virtual void Setup(IDocumentStore documentStore)
        {
            DocumentStore = documentStore;
        }

        public abstract void Up();

        protected void WaitForIndexing()
        {
            //DocumentStore.WaitForIndexing();
        }

        protected IDocumentStore DocumentStore { get; private set; }
    }
}