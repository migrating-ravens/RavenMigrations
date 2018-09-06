using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using System;
using System.Linq.Expressions;

namespace Raven.Migrations
{
    /// <summary>
    /// Base class for migrations.
    /// </summary>
    public abstract class Migration
    {
        protected IDocumentStore Db { get; private set; }
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Performs the upgrade.
        /// </summary>
        public abstract void Up();

        /// <summary>
        /// Undoes the upgrade.
        /// </summary>
        public virtual void Down()
        {
        }

        public virtual void Setup(IDocumentStore documentStore, ILogger logger)
        {
            Db = documentStore;
            Logger = logger;
        }

        /// <summary>
        /// Patches a single document.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity to patch.</typeparam>
        /// <typeparam name="TValue">The value to patch.</typeparam>
        /// <param name="entityId">The ID of the entity to patch.</param>
        /// <param name="propertySelector">The property selector that selects the property being patched.</param>
        /// <param name="newValue">The new value to set the property to.</param>
        /// <example>
        ///     <code>
        ///         // Patch orders/123-A to have a .Freight of 3.14
        ///         PatchDocument&lt;Order, double&gt;("orders/123-A", o => o.Freight, 3.14);
        ///     </code>
        /// </example>
        protected void PatchDocument<TEntity, TValue>(string entityId, Expression<Func<TEntity, TValue>> propertySelector, TValue newValue)
        {
            using (var session = Db.OpenSession())
            {
                session.Advanced.WaitForIndexesAfterSaveChanges(TimeSpan.FromSeconds(30), false);
                session.Advanced.Patch(entityId, propertySelector, newValue);
                session.SaveChanges();
            }
        }

        /// <summary>
        /// Patches a collection of documents using RQL.
        /// </summary>
        /// <param name="rql">The RQL code to patch.</param>
        /// <returns>The patch operation.</returns>
        /// <remarks>
        /// View all examples at https://ravendb.net/docs/article-page/4.0/csharp/client-api/operations/patching/set-based
        /// </remarks>
        /// <example>
        ///     <code>
        ///         // Patch all Orders to increase their Freight
        ///         PatchCollection("from Orders update { this.Freight += 10 }");
        ///     </code>
        /// </example>
        protected Operation PatchCollection(string rql, bool waitForCompletion = true)
        {
            var operation = this.Db.Operations.Send(new PatchByQueryOperation(rql));
            if (waitForCompletion)
            {
                operation.WaitForCompletion();
            }

            return operation;
        }
    }
}