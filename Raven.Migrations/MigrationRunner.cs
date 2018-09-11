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
        private readonly IDocumentStore docStore;
        private readonly MigrationOptions options;
        private readonly ILogger<MigrationRunner> logger;

        public MigrationRunner(IDocumentStore docStore, MigrationOptions options, ILogger<MigrationRunner> logger)
        {
            this.docStore = docStore ?? throw new ArgumentNullException(nameof(docStore));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Runs the pending migrations.
        /// </summary>
        public void Run()
        {
            var migrations = FindAllMigrationsWithOptions(options);

            foreach (var pair in migrations)
            {
                var migration = pair.Migration();
                migration.Setup(this.docStore, this.logger);
                var migrationId = migration.GetMigrationIdFromName(this.docStore.Conventions.IdentityPartsSeparator[0]);

                using (var session = this.docStore.OpenSession())
                {
                    var migrationDoc = session.Load<MigrationRecord>(migrationId);

                    switch (options.Direction)
                    {
                        case Directions.Down:
                            if (migrationDoc == null)
                                continue;

                            logger.LogInformation("{0}: Down migration started", migration.GetType().Name);
                            migration.Down();
                            session.Delete(migrationDoc);
                            logger.LogInformation("{0}: Down migration completed", migration.GetType().Name);
                            break;
                        default:
                            // we already ran it
                            if (migrationDoc != null)
                                continue;

                            logger.LogInformation("{0}: Up migration started", migration.GetType().Name);
                            migration.Up();
                            session.Store(new MigrationRecord { Id = migrationId });
                            logger.LogInformation("{0}: Up migration completed", migration.GetType().Name);
                            break;
                    }

                    session.SaveChanges();

                    if (pair.Attribute.Version == options.ToVersion)
                        break;
                }
            }
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
                where typeof(Migration).IsAssignableFrom(t)
                      && !t.IsAbstract
                      && t.GetConstructor(Type.EmptyTypes) != null
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
