using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;

namespace Raven.Migrations
{
    /// <summary>
    /// Allows for running migrations on a Raven database using a supplied <see cref="MigrationOptions"/>.
    /// </summary>
    public class MigrationRunner
    {
        private readonly IDocumentStore store;
        private readonly MigrationOptions options;
        private readonly ILogger<MigrationRunner> logger;

        public MigrationRunner(IDocumentStore store, MigrationOptions options, ILogger<MigrationRunner> logger)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Runs the pending migrations.
        /// </summary>
        public void Run()
        {
            var migrations = FindAllMigrationsWithOptions(options);
            var recordStore = options.MigrationRecordStore ?? new DefaultMigrationRecordStore(store, options);

            int runCount = 0;
            int skipCount = 0;
            foreach (var pair in migrations)
            {
                var migration = pair.Migration();
                migration.Setup(store, options, logger);
                var migrationId = options.Conventions.MigrationDocumentId(migration, store.Conventions.IdentityPartsSeparator[0]);
                var migrationDoc = recordStore.Load(migrationId);

                switch (options.Direction)
                {
                    case Directions.Down:
                        if (migrationDoc == null)
                        {
                            skipCount++;
                            continue;
                        }

                        logger.LogInformation("{0}: Down migration started", migration.GetType().Name);
                        migration.Down();
                        recordStore.Delete(migrationDoc);
                        logger.LogInformation("{0}: Down migration completed", migration.GetType().Name);
                        runCount++;
                        break;
                    default:
                        // we already ran it
                        if (migrationDoc != null)
                        {
                            skipCount++;
                            continue;
                        }

                        logger.LogInformation("{0}: Up migration started", migration.GetType().Name);
                        migration.Up();
                        recordStore.Store(migrationId);
                        logger.LogInformation("{0}: Up migration completed", migration.GetType().Name);
                        runCount++;
                        break;
                }

                if (pair.Attribute.Version == options.ToVersion)
                {
                    break;
                }
            }

            logger.LogInformation($"{runCount} migrations executed, {skipCount} skipped as unnecessary");
        }

        /// <summary>
        /// Returns all migrations found within all assemblies and orders them by the direction
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static IEnumerable<MigrationWithAttribute> FindAllMigrationsWithOptions(MigrationOptions options)
        {
            var migrationsToRun = 
                from assembly in options.Assemblies
                from t in assembly.GetLoadableTypes()
                where options.Conventions.TypeIsMigration(t)
                select new MigrationWithAttribute
                {
                    Migration = () => options.MigrationResolver.Resolve(t),
                    Attribute = t.GetMigrationAttribute()
                } into migration
                where IsInCurrentMigrationProfile(migration, options)
                select migration;

            // if we are going down, we want to run it in reverse
            return options.Direction == Directions.Down 
                ? migrationsToRun.OrderByDescending(x => x.Attribute.Version) 
                : migrationsToRun.OrderBy(x => x.Attribute.Version);
        }

        private static bool IsInCurrentMigrationProfile(MigrationWithAttribute migrationWithAttribute, MigrationOptions options)
        {
            if (migrationWithAttribute.Attribute == null)
            {
                throw new InvalidOperationException("Subclasses of Migration that can be instantiated must have the MigrationAttribute." +
                                                    "If this class was intended as a base class for other migrations, make it an abstract class.");
            }

            //If no particular profiles have been set, then the migration is
            //effectively a part of all profiles
            var profiles = migrationWithAttribute.Attribute.Profiles;
            if (profiles.Any() == false)
                return true;

            //The migration must belong to at least one of the currently 
            //specified profiles
            return options.Profiles
                .Intersect(migrationWithAttribute.Attribute.Profiles, StringComparer.OrdinalIgnoreCase)
                .Any();
        }
    }
}
