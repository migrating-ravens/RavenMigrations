using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Raven.Client;

namespace RavenMigrations
{
    public class Runner
    {
        public static void Run(IDocumentStore documentStore, MigrationOptions options = null)
        {
            if (options == null)
                options = new MigrationOptions();

            if (!options.Assemblies.Any())
                options.Assemblies.Add(Assembly.GetCallingAssembly());

            var migrations = FindAllMigrationsWithOptions(options);

            foreach (var pair in migrations)
            {
                // send in the document Store
                var migration = pair.Migration();
                migration.Setup(documentStore, options.Logger);

                // todo: possible issue here with sharding
                var migrationId = 
                    migration.GetMigrationIdFromName(documentStore.Conventions.IdentityPartsSeparator[0]);

                using (var session = documentStore.OpenSession())
                {
                    var migrationDoc = session.Load<MigrationDocument>(migrationId);

                    switch (options.Direction)
                    {
                        case Directions.Down:
                            options.Logger.WriteInformation("{0}: Down migration started", migration.GetType().Name);
                            migration.Down();
                            session.Delete(migrationDoc);
                            options.Logger.WriteInformation("{0}: Down migration completed", migration.GetType().Name);
                            break;
                        default:
                            // we already ran it
                            if (migrationDoc != null)
                                continue;

                            options.Logger.WriteInformation("{0}: Up migration started", migration.GetType().Name);
                            migration.Up();
                            session.Store(new MigrationDocument { Id = migrationId });
                            options.Logger.WriteInformation("{0}: Up migration completed", migration.GetType().Name);
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
            var migrations = new List<MigrationWithAttribute>();
            foreach (var assembly in options.Assemblies)
            {
                var migrationsFromAssembly =
                    from t in assembly.GetLoadableTypes()
                    where typeof(Migration).IsAssignableFrom(t)
                    select new MigrationWithAttribute
                    {
                        Migration = () => options.MigrationResolver.Resolve(t),
                        Attribute = t.GetMigrationAttribute()
                    };

                migrations.AddRange(migrationsFromAssembly);
            }

            var migrationsToRun = from m in migrations
                                  where IsInCurrentMigrationProfile(m, options)
                                  orderby m.Attribute.Version
                                  select m;

            // if we are going down, we want to run it in reverse
            if (options.Direction == Directions.Down)
                migrationsToRun = migrationsToRun.OrderByDescending(x => x.Attribute.Version);

            return migrationsToRun;
        }

        private static bool IsInCurrentMigrationProfile(MigrationWithAttribute migrationWithAttribute, MigrationOptions options)
        {
            return string.IsNullOrWhiteSpace(migrationWithAttribute.Attribute.Profile) ||
            options.Profiles.Any(x => StringComparer.InvariantCultureIgnoreCase.Compare(migrationWithAttribute.Attribute.Profile, x) == 0);
        }
    }
}
