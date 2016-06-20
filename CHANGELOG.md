# Change Log
All notable changes will be documented in this file, particularly any breaking
changes. This project adheres to [Semantic Versioning](http://semver.org).

## [x.x.x]

## [2.1.0]
- Added - Option to configure a `RavenMigration.ILogger` or use the `ConsoleLogger`.

## [2.0.0]
- Changed (breaking) - The way the MigrationDocument's Id is determined. Multiple underscores
  will now combined into one separator to be used between each section of the Id.
  **This could be a breaking change if there are migrations named with multiple
  underscores**, depending on the version of Raven client and server in use. Some
  versions of Raven ignore multiple separators and others do not. This change
  **could cause migrations to be run multiple times** in some cases if the ids are not
  changed first.
- Added - Ability to inherit from `MigrationAttribute` to specify custom migration
  versions.

## [1.2.0]
- Added new way to change a collection with
  ```Alter.CollectionWithAdditionalCommands```. This works the same way as
  ```Collection``` except you can return additional commands from the ```Func```
  that will be run the in the same transaction as the collection's document.

## [1.1.0]
- Fixed - Using a base class for migrations would throw an unhelpful error.
  Migrations that inherit from ```Migration``` and don't have the
  ```MigrationAttribute``` will now cause an ```InvalidOperationException``` to be
  thrown with a more helpful error message. Abstract migrations and those without
  parameterless constructors will be filtered by the ```Runner```. So, the
  accepted way of base-classing migrations is to make the base class abstract.
- Fixed - When the index changes during a migration it could cause documents to be
  missed. Changed to use Raven's streaming API for ```Alter.Collection()```. This
  will ensure that even if the index changes during the migration, the migration
  will not be affected.
- Added - Ability to specify multiple migration profiles per migration.
