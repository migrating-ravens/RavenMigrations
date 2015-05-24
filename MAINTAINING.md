# Maintaining
The following is a list of guidelines for maintainers of this project.

## Publishing
1. Add a commit to bump the version in the appveyor.yml and update the changelog.
  - Ideally the changelog will already have been updated with noteworthy changes per the [contributing guide](./CONTRIBUTING.md).
  - Change the x.x.x version in the changelog to the release version.
  - Add a new x.x.x section in the changelog.
1. Tag the commit as the new version.
1. AppVeyor should handle the rest. The configuration in the appveyor.yml is set up to deploy to Nuget.org on successful tagged builds.

## Versioning
Follow [semver](http://semver.org) for all version changes. The version for this
project should be controlled by the settings in the ```appveyor.yml``` file. There
is an environment variable that sets the core version, which is usually the only
place the version would need to be changed. For prerelease or other version
formats based on the semver guidelines, the ```assembly_informational_version```
would need to be changed because ```assembly_version``` does not support these
semver formats.
