using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Commands;
using Raven.Client.Documents.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Raven.Migrations
{
    /// <summary>
    /// Base class for migrations.
    /// </summary>
    public abstract class Migration
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable. Hiding this warning, as this value is set immediately upon dynamic creation of migrations.
        /// <summary>
        /// The Raven document store. This will be non-null after setup is called.
        /// </summary>
        protected IDocumentStore DocumentStore { get; private set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        
        /// <summary>
        /// The name of the database to operate on.
        /// </summary>
        protected string? Database { get; private set; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable. This field is set during dynamic creation of migrations.        
        /// <summary>
        /// The logger. This will be non-null after setup is called.
        /// </summary>
        protected ILogger Logger { get; private set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

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

        public virtual void Setup(
            IDocumentStore documentStore,
            MigrationOptions options,
            ILogger logger)
        {
            DocumentStore = documentStore;
            Database = options.Database;
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
        /// <param name="timeout">The timeout to wait for indexes after patching. If null, defaults to 30 seconds.</param>
        /// <example>
        ///     <code>
        ///         // Patch orders/123-A to have a .Freight of 3.14
        ///         PatchDocument&lt;Order, double&gt;("orders/123-A", o => o.Freight, 3.14);
        ///     </code>
        /// </example>
        protected void PatchDocument<TEntity, TValue>(string entityId, Expression<Func<TEntity, TValue>> propertySelector, TValue newValue, TimeSpan? timeout = null)
        {
            var timeoutVal = timeout ?? TimeSpan.FromSeconds(30);
            using var session = DocumentStore.OpenSession(Database);
            session.Advanced.WaitForIndexesAfterSaveChanges(timeoutVal, false);
            session.Advanced.Patch(entityId, propertySelector, newValue);
            session.SaveChanges();
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
            var operation = DocumentStore.Operations.ForDatabase(Database).Send(new PatchByQueryOperation(rql));
            if (waitForCompletion)
            {
                operation.WaitForCompletion();
            }

            return operation;
        }

        /// <summary>
        /// Lazily streams in a collection.
        /// WARNING: Avoid calling .ToList or otherwise causing the whole collection to be in memory, as loading very large collections into memory can crash your process.
        /// </summary>
        /// <typeparam name="T">The type of items to return.</typeparam>
        /// <returns>A lazy <see cref="IEnumerable{T}"/> of items in the collection that match the predicate.</returns>
        protected IEnumerable<T> Stream<T>()
        {
            return StreamWithMetadata<T>().Select(r => r.Document);
        }

        /// <summary>
        /// Lazily streams in a collection, returning the documents and their metadata.
        /// WARNING: Avoid calling .ToList or otherwise causing the whole collection to be in memory, as loading very large collections into memory can crash your process.
        /// </summary>
        /// <typeparam name="T">The type of items to return.</typeparam>
        /// <returns>A lazy <see cref="IEnumerable{T}"/> of items in the collection that match the predicate.</returns>
        protected IEnumerable<StreamResult<T>> StreamWithMetadata<T>()
        {
            using (var dbSession = DocumentStore.OpenSession(Database))
            {
                var collectionName = dbSession.Advanced.DocumentStore.Conventions.GetCollectionName(typeof(T));
                var separator = dbSession.Advanced.DocumentStore.Conventions.IdentityPartsSeparator;
                using var enumerator = dbSession.Advanced.Stream<T>(collectionName + separator);
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
        }
    }
}