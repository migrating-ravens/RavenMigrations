using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using System;
using System.Reflection;

namespace Raven.Migrations
{
    /// <summary>
    /// Extends the <see cref="IServiceCollection"/> so that RavenDB services can be registered through it.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a Raven <see cref="MigrationRunner"/> singleton to the dependency injection container. Uses the <see cref="IDocumentStore"/> inside the dependency injection container.
        /// </summary>
        /// <param name="services">The dependency injection container.</param>
        /// <param name="configuration">A function that configures the migration options.</param>
        /// <returns>A <see cref="MigrationRunner"/> which can be used to run the pending migrations.</returns>
        public static IServiceCollection AddRavenDbMigrations(this IServiceCollection services, Action<MigrationOptions> configuration = null)
        {
            var options = new MigrationOptions();
            configuration?.Invoke(options);
            if (options.Assemblies.Count == 0)
            {
                options.Assemblies.Add(Assembly.GetCallingAssembly());
            }
            
            return services.AddSingleton(provider =>
            {
                var docStore = provider.GetRequiredService<IDocumentStore>();
                var logger = provider.GetRequiredService<ILogger<MigrationRunner>>();
                return new MigrationRunner(docStore, options, logger);
            });
        }

        /// <summary>
        /// Adds a Raven <see cref="MigrationRunner"/> singleton to the dependency injection services using the specified <see cref="IDocumentStore"/>.
        /// </summary>
        /// <param name="services">The dependency injection container.</param>
        /// <returns>A <see cref="MigrationRunner"/> which can be used to run the pending migrations.</returns>
        public static IServiceCollection AddRavenDbMigrations(this IServiceCollection services, IDocumentStore docStore, Action<MigrationOptions> configuration = null)
        {
            var options = new MigrationOptions();
            configuration?.Invoke(options);
            if (options.Assemblies.Count == 0)
            {
                options.Assemblies.Add(Assembly.GetCallingAssembly());
            }

            return services.AddSingleton(provider => new MigrationRunner(docStore, options, provider.GetRequiredService<ILogger<MigrationRunner>>()));
        }
    }
}
