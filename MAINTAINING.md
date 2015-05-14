# Maintaining
The following is a list of guidelines for maintainers of this project.

## Versioning
Follow [semver](http://semver.org) for all version changes. The version for this
project should be controlled by the settings in the ```appveyor.yml``` file. There
is an environment variable that sets the core version, which is usually the only
place the version would need to be changed. For prerelease or other version
formats based on the semver guidelines, the ```assembly_informational_version```
would need to be changed because ```assembly_version``` does not support these
semver formats.
