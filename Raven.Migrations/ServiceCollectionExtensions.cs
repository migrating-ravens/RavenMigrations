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
        /// <param name="singleInstance"> Makes sure no more than one migration can be executed per backing database at the time.</param>
        /// <returns>A <see cref="MigrationRunner"/> which can be used to run the pending migrations.</returns>
        public static IServiceCollection AddRavenDbMigrations(this IServiceCollection services)
        {
            return CreateMigrationRunner(services, null, null, Assembly.GetCallingAssembly());
        }

        /// <summary>
        /// Adds a Raven <see cref="MigrationRunner"/> singleton to the dependency injection container. Uses the <see cref="IDocumentStore"/> inside the dependency injection container.
        /// </summary>
        /// <param name="services">The dependency injection container.</param>
        /// <param name="configuration">A function that configures the migration options.</param>
        /// <param name="singleInstance"> Makes sure no more than one migration can be executed per backing database at the time.</param>
        /// <returns>A <see cref="MigrationRunner"/> which can be used to run the pending migrations.</returns>
        public static IServiceCollection AddRavenDbMigrations(this IServiceCollection services, Action<MigrationOptions> configuration)
        {
            return CreateMigrationRunner(services, configuration, null, Assembly.GetCallingAssembly());
        }

        /// <summary>
        /// Adds a Raven <see cref="MigrationRunner"/> singleton to the dependency injection services using the specified <see cref="IDocumentStore"/>.
        /// </summary>
        /// <param name="services">The dependency injection container.</param>
        /// <param name="configuration">An action that sets the migration configuration. Can be null.</param>
        /// <param name="docStore">The <see cref="IDocumentStore"/> to run the migrations against. Can be null. If null, an <see cref="IDocumentStore"/> must be available in the DI container.</param>
        /// <param name="singleInstance"> Makes sure no more than one migration can be executed per backing database at the time.</param>
        /// <returns>A <see cref="MigrationRunner"/> which can be used to run the pending migrations.</returns>
        public static IServiceCollection AddRavenDbMigrations(this IServiceCollection services, Action<MigrationOptions> configuration, IDocumentStore docStore)
        {
            return CreateMigrationRunner(services, configuration, docStore, Assembly.GetCallingAssembly());
        }

        private static IServiceCollection CreateMigrationRunner(
            IServiceCollection services, 
            Action<MigrationOptions>? configuration, 
            IDocumentStore? docStore, 
            Assembly assembly)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetEntryAssembly();
            }

            return services.AddSingleton(provider => CreateMigrationRunnerFromProvider(provider, assembly, configuration, docStore));
        }

        private static MigrationRunner CreateMigrationRunnerFromProvider(IServiceProvider provider, Assembly callingAssembly, Action<MigrationOptions>? configuration = null, IDocumentStore? store = null)
        {
            var migrationResolver = new DependencyInjectionMigrationResolver(provider);
            var options = new MigrationOptions(migrationResolver);
            configuration?.Invoke(options);
            if (options.Assemblies.Count == 0)
            {
                // No assemblies configured? Use the assembly that called .AddRavenDbMigrations.
                options.Assemblies.Add(callingAssembly);
            }

            var docStore = store ?? provider.GetRequiredService<IDocumentStore>();
            var logger = provider.GetRequiredService<ILogger<MigrationRunner>>();
            return new MigrationRunner(docStore, options, logger);
        }
    }
}
