using System;

namespace Raven.Migrations
{
    public interface IMigrationResolver
    {
        Migration Resolve(Type migrationType);
    }
}