# Raven Migrations

[![Join the chat at https://gitter.im/migrating-ravens/RavenMigrations](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/migrating-ravens/RavenMigrations?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[![Build status](https://ci.appveyor.com/api/projects/status/4emkngqp8xk2k96j?svg=true)](https://ci.appveyor.com/project/dportzline83/ravenmigrations)

## Quick Start

```
    PM > Install-Package RavenDB.Migrations
```

## Introduction

Raven Migrations is a migration framework for [RavenDB](http://ravendb.net) to help with common tasks you might have to do over time to your database. The framework API is heavily influenced by [Fluent Migrator](https://github.com/schambers/fluentmigrator).

## Philosophy

We believe any changes to your domain should be visible in your code and reflected as such. Changing things "on the fly", can lead to issues, where as migrations can be tested and throughly vetted before being exposed into your production environment. With RavenDB testing migrations is super simple since RavenDB supports in memory databases (our test suite is in memory).

This is important, once a migration is in your production environment, **NEVER EVER** modify it in your code. Treat a migration like a historical record of changes.

## Concepts

Every migration has several elements you need to be aware of. Additionally, there are over arching concepts that will help you structure your project to take advantage of this library.

### A Migration

A migration looks like the following:

```csharp
// #1 - specify the migration number
[Migration(1)]                 
public class PeopleHaveFullNames : Migration // #2 inherit from Migration
{
    // #3 Do the migration using RQL.
    public override void Up()
    {
        this.PatchCollection(@"
            from People as person
            update {
                person.FullName = person.FirstName + ' ' + person.LastName;
            }
        ");
    }
    // #4 optional: undo the migration
    public override void Down()
    {
        this.PatchCollection(@"
            from People as person
            update {
                delete person.FullName;
            }
        ");
    }
}
```

To run the migrations, here's how it'd look in an ASP.NET Core app.

```csharp
// In Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Add the MigrationRunner into the dependency injection container.
    services.AddRavenDbMigrations();
}

public void Configure(IApplicationBuilder app, ...)
{
    // Run pending Raven migrations.
    var migrationService = app.ApplicationServices.GetRequiredService<MigrationRunner>();
    migrationService.Run();
}
```

Not using ASP.NET Core? You can create the runner manually:
```csharp
// Skip dependency injection and run the migrations.

// Create migration options, using all Migration objects found in the current assembly.
var options = new MigrationOptions();
options.Assemblies.Add(Assembly.GetExecutingAssembly());

// Create a new migration runner. docStore is your RavenDB IDocumentStore. Logger is an ILogger<MigrationRunner>.
var migrationRunner = new MigrationRunner(docStore, options, logger);
migrationRunner.Run();
```

Each important part of the migration is numbered:

1. Every migration has to be decorated with the **MigrationAttribute**, and needs to be seeded it with a *long* value. For smaller teams, a simple integer will do (ex. the first patch has Migration(1), then the second patch has Migration(2), etc.) If you're working with a larger team, where patch numbers might collide, we recommend you seed it with a **DateTime** stamp of yyyyMMddHHmmss ex. 20131031083545. This helps keeps teams from guessing and conflicting on the next migration number.
2. Every migration needs to implement from the base class of **Migration**. This gives you access to base functionality and the ability to implement **Up** and **Down**. It also gives you access to the Raven ``DocumentStore`` and an ``ILogger`` instance.
3. **Up** is the method that occurs when a migration is executed. As you see above, we are adding a document.
4. **Down** is the method that occurs when a migration is rolledback. This is not always possible, but if it is, then it most likely will be the reverse of **Up**.

In every migration you have access to the document store, so you are able to do anything you need to your storage engine. This document store is the same as the one your application will use.

### Runner

Raven Migrations comes with a migration runner. It scans all provided assemblies for any classes implementing the **Migration** base class and then orders them according to their migration value.

After each migration is executed, a document of type **MigrationDocument** is inserted into your database, to ensure the next time the runner is executed that migration is not executed again. When a migration is rolled back the document is removed.

You can modify the runner options by passing an action to the .AddRavenDbMigrations call:

```csharp
services.AddRavenDbMigrations(options =>
{
   // Configure the migration options here
});
```

### Profiles

We understand there are times when you want to run specific migrations in certain environments, so Raven Migrations supports profiles. For instance, some migrations might only run during development, by decorating your migration with the profile of *"development"* and setting the options to include the profile will execute that migration.

```csharp
[Migration(3, "development")]
public class Development_Migration : Migration
{
    public override void Up()
    {
        using (var session = Db.OpenSession())
        {
            session.Store(new { Id = "development-1" });
            session.SaveChanges();
        }
    }
}

...
// Add the MigrationRunner and configure it to run development migrations only.
services.AddRavenDbMigrations(options => options.Profiles = new[] { "development" } });

```

You can also specify that a particular profile belongs in more than one profile by setting multiple profile names in the attribute.

``
[Migration(3, "development", "demo")]
``

This migration would run if either (or both) the development and demo profiles were specified in the MigrationOptions.

### Advanced Migrations
Inside each of your Migration instances, you should use RavenDB's <a href="https://ravendb.net/docs/article-page/4.0/csharp/client-api/operations/patching/set-based">patching APIs</a> to perform updates to collections and documents. We also provide helper methods on the Migration class for easy access, see below for examples.

#### Migration.PatchCollection
```Migration.PatchCollection``` is a helper method that <a href="https://ravendb.net/docs/article-page/4.0/csharp/client-api/operations/patching/set-based">patches a collection via RQL</a>.

```csharp
public override void Up()
{
   this.PatchCollection("from People update { p.Foo = 'Hello world!' }");
}
```

#### Migrations using dependency injection services
```csharp
[Migration(1)]
public class MyMigrationUsingServices : Migration
{
	private IFoo foo;

	// Inject an IFoo for use in our patch.
	public MyMigrationUsingServices(IFoo foo)
	{
		this.foo = foo;
	}

	public override void Up()
	{
		// Do something with foo
	}
}
```

#### Example: Adding and deleting properties
Let's say you start using a single Name property:

```csharp
public class Person
{
    public string Id { get; set; }
    public string Name { get; set; }
}
```
But then want to change using two properties, FirstName and LastName:
```csharp
public class Person
{
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```
You now need to migrate your documents or you will lose data when you load your new ```Person```.  The following migration uses RQL to split out the first and last names:

```csharp
[Migration(1)]
public class PersonNameMigration : Migration
{
    public override void Up()
    {
        this.PatchCollection(@"
            from People as p
            update {
                var names = p.Name.split(' ');
                p.FirstName = names[0];
                p.LastName = names[1];
                delete p.Name;
            }
        ");
    }

    // Undo the patch
    public override void Down()
    {
        this.PatchCollection("this.Name = this.FirstName + ' ' + this.LastName;");
    }
}
```

## Integration

We suggest you run the migrations at the start of your application to ensure that any new changes you have made apply to your application before you application starts. If you do not want to do it here, you can choose to do it out of band using a seperate application. If you're using ASP.NET Core, you can run them in your Startup.cs

```csharp
    public void ConfigureServices(IServiceCollection services)
    {
        // Add the MigrationRunner singleton into the dependency injection container.
        services.AddRavenDbMigrations();

        // ...
   
        // Get the migration runner and execute pending migrations.
        var migrationRunner = services.BuildServiceProvider().GetRequiredService<MigrationRunner>();
        migrationRunner.Run();
    }
```

Not using ASP.NET Core? You can create the runner manually:
```csharp
    // Skip dependency injection and run the migrations.

    // Create migration options, using all Migration objects found in the current assembly.
    var options = new MigrationOptions();
    options.Assemblies.Add(Assembly.GetExecutingAssembly());

    // Create a new migration runner. docStore is your RavenDB IDocumentStore. Logger is an ILogger<MigrationRunner>.
    var migrationRunner = new MigrationRunner(docStore, options, logger);
    migrationRunner.Run();
```

### Solution Structure

We recommend you create a folder called Migrations, then name your files according to the migration number and name:

```
\Migrations
    - 001_FirstMigration.cs
    - 002_SecondMigration.cs
    - 003_ThirdMigration.cs
```

The advantage to this approach, is that your IDE will order the migrations alpha-numerically allowing you to easily find the first and last migration.

## Contributing

Contributions of any size are always welcome! Please read our [Code of Conduct](./CODE_OF_CONDUCT.md) and [Contribution Guide](./CONTRIBUTING.md) and then jump in!

## Thanks

Thanks goes to [Sean Kearon](https://github.com/seankearon) who helped dog food this migration framework and contribute to it. Also to Darrel Portzline and Khalid Abuhakmeh for their work on an earlier version of this project.

## Versioning

This project strives to adhere to the [semver](http://semver.org) guidelines. See the [contributing](./CONTRIBUTING.md) and [maintaining](./MAINTAINING.md)
guides for more on this.
