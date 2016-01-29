using System;
using System.Diagnostics;
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

        public virtual void Setup(IDocumentStore documentStore, Action<string> logger = null)
        {
            DocumentStore = documentStore;
            Alter = new Alter(documentStore);
            _logger = logger;
        }

        public abstract void Up();

        protected void WaitForIndexing()
        {
            DocumentStore.WaitForIndexing();
        }

        protected void Log(string message)
        {
            if (_logger != null)
                _logger(message);
            else
                Debug.WriteLine(message);
        }
        protected Alter Alter { get; private set; }
        protected IDocumentStore DocumentStore { get; private set; }

        private Action<string> _logger;
    }
}