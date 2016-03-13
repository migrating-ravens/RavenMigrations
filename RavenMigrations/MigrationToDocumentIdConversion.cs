namespace RavenMigrations
{
    public delegate string MigrationToDocumentIdConversion(Migration migration, char defaultIdentityPartsSeparator);
}