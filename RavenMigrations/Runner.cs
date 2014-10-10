using System;
using System.Linq;
using System.Reflection;
using Raven.Client;

namespace RavenMigrations
{
    public class Runner
    {
        public static void Run(IDocumentStore documentStore, MigrationOptions options = null, IMigrationCollector migrationCollector = null)
        {
            if (options == null)
                options = new MigrationOptions();

            if (!options.Assemblies.Any())
                options.Assemblies.Add(Assembly.GetCallingAssembly());

            if (migrationCollector == null)
                migrationCollector = new AttributeBasedMigrationCollector(options.MigrationResolver,
                    AttributeBasedMigrationCollector.AssemblyScannerBuilder(options.Assemblies));
            
            var migrations = migrationCollector.GetOrderedMigrations(options.Profiles);

            // if we are going down, we want to run it in reverse
            if (options.Direction == Directions.Down)
                migrations = migrations.OrderByDescending(x => x.Properties.Version);
            else if(options.Direction == Directions.Up)
                migrations = migrations.OrderBy(x => x.Properties.Version);


            foreach (var pair in migrations)
            {
                // send in the document Store
                var migration = pair.Migration();
                migration.Setup(documentStore);

                // todo: possible issue here with sharding
                var migrationId =
                    pair.GetMigrationId(documentStore.Conventions.IdentityPartsSeparator[0]);

                using (var session = documentStore.OpenSession())
                {
                    var migrationDoc = session.Load<MigrationDocument>(migrationId);
                    if (migrationDoc != null && migrationDoc.HasError && !migrationDoc.Error.IsFixed)
                    {
                        return;
                    }

                    switch (options.Direction)
                    {
                        case Directions.Down:
                            // we never ran it
                            if (migrationDoc == null)
                                return;
                            migration.Down();
                            session.Delete(migrationDoc);
                            break;
                        default:
                            // we already ran it
                            if (migrationDoc != null)
                                continue;
                            var migrationDocument = new MigrationDocument { Id = migrationId };
                            session.Store(migrationDocument);

                            try
                            {
                                migration.Up();
                            }
                            catch (Exception e)
                            {
                                migrationDocument.CaptureException(e, options.Direction);
                                session.SaveChanges();
                                return;
                            }
                            break;
                    }

                    session.SaveChanges();

                    if (options.ToVersion != null && pair.Properties.Version == options.ToVersion)
                        break;
                }
            }
        }
    }
}
