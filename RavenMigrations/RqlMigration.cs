using System;
using System.Collections.Generic;
using System.Text;

namespace Raven.Migrations
{
    /// <summary>
    /// A migration that uses RQL to patch documents.
    /// </summary>
    /// <remarks>
    /// For examples, see https://ravendb.net/docs/article-page/4.0/Csharp/client-api/operations/patching/set-based
    /// </remarks>
    public abstract class RqlMigration : Migration
    {
        /// <summary>
        /// Gets or sets the RQL patch script.
        /// </summary>
        /// <example>
        ///     <code>
        ///         this.Rql = @"from Products as p
        ///                      where p.Supplier = 'suppliers/12-A'
        ///                      update
        ///                      {
        ///                         p.Supplier = 'suppliers/13-A'
        ///                      }
        ///                     ";
        ///     </code>
        /// </example>
        public string Rql { get; set; }
    }
}
