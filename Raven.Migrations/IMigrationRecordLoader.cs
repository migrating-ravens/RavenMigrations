namespace Raven.Migrations
{
    public interface IMigrationRecordStore
    {
        IMigrationRecord Load(string migrationId);
        void Store(string migrationId);
        void Delete(IMigrationRecord record);
    }
}