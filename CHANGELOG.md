# Change Log
All notable changes will be documented in this file, particularly any breaking
changes. This project adheres to [Semantic Versioning](http://semver.org).

## [x.x.x]

- Migrations that inherit from ```Migration``` but don't have the
  ```MigrationAttribute``` will now be filtered out of the migration process
  instead of throwing an error.
- Use Raven's streaming API for altering collections with
  ```Alter.Collection()```. This will ensure that even if the index changes during
  the migration, the migration will not be affected.

