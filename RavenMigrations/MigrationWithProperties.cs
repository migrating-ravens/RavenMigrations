using System;

namespace RavenMigrations
{
    public class MigrationWithProperties
    {
        public Type MigrationType { get; set; }
        public Func<Migration> Migration { get; set; }
        public MigrationProperties Properties { get; set; }

        public static MigrationWithProperties FromTypeWithAttribute(Type t, IMigrationResolver resolver)
        {
            return  new MigrationWithProperties
                        {
                            MigrationType = t,
                            Migration = () => resolver.Resolve(t),
                            Properties = t.GetMigrationPropertiesFromAttribute()
                        };
        }
    }
}