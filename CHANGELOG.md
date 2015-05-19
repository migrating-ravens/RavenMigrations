# Change Log
All notable changes will be documented in this file, particularly any breaking
changes. This project adheres to [Semantic Versioning](http://semver.org).

## [x.x.x]


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
