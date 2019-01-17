using System;
using System.Collections.Generic;
using System.Text;

namespace Raven.Migrations
{
    /// <summary>
    /// Creates <see cref="Migration"/> instances using .NET Core's dependency injection (DI) container.
    /// This allows users to create Migrations that rely on DI services.
    /// </summary>
    public class DependencyInjectionMigrationResolver : IMigrationResolver
    {
        private readonly IServiceProvider serviceProvider;

        public DependencyInjectionMigrationResolver(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public Migration Resolve(Type migrationType)
        {
            return (Migration)Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance(this.serviceProvider, migrationType);
        }
    }
}
