using Raven.Client.Documents;

namespace Raven.Migrations
{
    public class DefaultMigrationRecordStore : IMigrationRecordStore
    {
        private readonly IDocumentStore store;
        private readonly MigrationOptions options;

        public DefaultMigrationRecordStore(
            IDocumentStore store,
            MigrationOptions options)
        {
            this.store = store;
            this.options = options;
        }

        public IMigrationRecord Load(string migrationId)
        {
            using (var session = store.OpenSession(options.Database))
            {
                return session.Load<MigrationRecord>(migrationId);
            }
        }

        public void Delete(IMigrationRecord record)
        {
            using (var session = store.OpenSession(options.Database))
            {
                session.Delete(record.Id);
                session.SaveChanges();
            }
        }

        public void Store(string migrationId)
        {
            using (var session = store.OpenSession(options.Database))
            {
                session.Store(new MigrationRecord
                {
                    Id = migrationId
                });
                session.SaveChanges();
            }
        }
    }
}