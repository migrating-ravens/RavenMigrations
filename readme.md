# Raven Migrations

## Quick Start

```
    PM > Install-Package RavenMigrations
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

```
	// #1
    [Migration(1)]                 
    public class First_Migration : Migration // #2
    {
    	// #3
        public override void Up()
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new TestDocument { Name = "Khalid Abuhakmeh" });
                session.SaveChanges();
            }
        }
        // #4
        public override void Down()
        {
            DocumentStore.DatabaseCommands.DeleteByIndex(new TestDocumentIndex().IndexName, new IndexQuery());
        }
    }
```

Each important part of the migration is numbered:

1. Every migration has to be decorated with the **MigrationAttribute**, and needs to be seeded it with a **long* value. We recommend you seed it with a **DateTime** stamp of yyyyMMddHHmmss ex. 20131031083545. This helps keeps teams for guessing and conflicting on the next migration number.
2. Every migration needs to implement from the base class of **Migration**. This gives you access to base functionality and the ability to implement **Up** and **Down**.
3. **Up** is the method that occurs when a migration is executed. As you see above, we are adding a document.
4. **Down** is the method that occurs when a migration is rolledback. This is not always possible, but if it is, then it most likely will be the reverse of **Up**.

In every migration you have access to the document store, so you are able to do anything you need to your storage engine. This document store is the same as the one your application will use.

### Runner

Raven Migrations comes with a migration runner. It scans all provided assemblies for any classes implementing the **Migration** base class and then orders them according to their migration value. 

After each migration is executed, a document of type **MigrationDocument** is inserted into your database, to insure the next time the runner is executed that migration is not executed again. When a migration is rolled back the document is removed.

You can modify the runner options by declaring a **MigrationOptions** instance and passing it to the runner.

```
 	public class MigrationOptions
    {
        public MigrationOptions()
        {
            Direction = Directions.Up;
            Assemblies = new List<Assembly>();
            Profiles = new List<string>();
            MigrationResolver = new DefaultMigrationResolver();
            Assemblies = new List<Assembly>();
            ToVersion = 0;
        }

        public Directions Direction { get; set; }
        public IList<Assembly> Assemblies { get; set; }
        public IList<string> Profiles { get; set; }
        public IMigrationResolver MigrationResolver { get; set; }
        public long ToVersion { get; set; }
    }
```

### Profiles

We understand there are times when you want to run specific migrations in certain environments, so Raven Migrations supports profiles. For instance, some migrations might only run during development, by decorating your migration with the profile of *"development"* and setting the options to include the profile will execute that migration.

```
	[Migration(3, "development")]
    public class Development_Migration : Migration
    {
        public override void Up()
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new { Id = "development-1" });
                session.SaveChanges();
            }
        }
    }

    Runner.Run(store, new MigrationOptions { Profiles = new[] { "development" } });
```

You can also specify that a particular profile belongs in more than one profile by comma-separating the profile names in the attribute.

``
[Migration(3, "development, demo")]
``

This migration would run if either (or both) the development and demo profiles were specified in the MigrationOptions.

### Advanced Migrations
Raven Migrations lets you migrate at the **RavenJObject** level, giving full access to the document and metadata.  This closely follows [Ayende's](https://github.com/ayende) approach porting the [MVC Music Store](http://ayende.com/blog/4519/porting-mvc-music-store-to-raven-advanced-migrations).  

#### Alter.Collection
```Alter.Collection``` works on a collection and gives access to the document and metadata:

```
Alter.Collection("People", (doc, metadata) => { ... });
```

Batching changes is taken care of with the default batch size being 128.  You can change the batch size if needed: 
```
public void Collection(string tag, Action<RavenJObject, RavenJObject> action, int pageSize = 128)
```

#### Example 1
Let's say you start using a single property:

```
    public class Person
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
```
But then want to change using two properties:
```
    public class Person
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
```
You now need to migrating your documents or you will lose data when you load your new ```Person```.  The following migration uses ```Alter.Collection``` to split out the first and last names:   

```
    [Migration(1)]
    public class PersonNameMigration : Migration
    {
        public override void Down()
        {
            Alter.Collection("People", MigratePerson2ToPerson1);
        }

        public override void Up()
        {
            Alter.Collection("People", MigratePerson1ToPerson2);
        }

        private void MigratePerson2ToPerson1(RavenJObject doc, RavenJObject metadata)
        {
            var first = doc.Value<string>("FirstName");
            var last = doc.Value<string>("LastName");

            doc["Name"] = first + " " + last;
            doc.Remove("FirstName");
            doc.Remove("LastName");
        }

        private void MigratePerson1ToPerson2(RavenJObject doc, RavenJObject metadata)
        {
            var name = doc.Value<string>("Name");
            if (!string.IsNullOrEmpty(name))
            {
                doc["FirstName"] = name.Split(' ')[0];
                doc["LastName"] = name.Split(' ')[1];
            }
            doc.Remove("Name");
        }
    }
```

#### Working with Metadata
Let's say that you refactor and move ```Person``` to another assembly.  So that RavenDB will load the data into the new class, you will need to adjust the metadata in the collection for the new CLR type.

```
    [Migration(2)]
    public class MovePersonMigration : Migration
    {
        public override void Up()
        {
            Alter.Collection("People",
                (doc, metadata) =>
                {
                    metadata[Constants.RavenClrType] = "MyProject.Person, MyProject";
                });
        }

        public override void Down()
        {
            Alter.Collection("People",
                (doc, metadata) =>
                {
                    metadata[Constants.RavenClrType] = "MyProject.Domain.Person, Domain";
                });
        }
    }
```

## Integration

We suggest you run the migrations at the start of your application to ensure that any new changes you have made apply to your application before you application starts. If you do not want to do it here, you can choose to do it out of band using a seperate application.

### Solution Structure

We recommend you create a folder called Migrations, then name your files according to the migration long value. For example

```
\Migrations
    - 20131010120000_FirstMigration.cs
    - 20131010120001_SecondMigration.cs
    - 20131110120001_ThirdMigration.cs
    - etc....
```

The advantage to this approach, is that your IDE will order the migrations alpha-numerically allowing you to easily find the first and last migration.

## Gotchas

1. If you use a domain model in your migration, be prepared for that migration to break if properties are removed critical to the migration. There are ways to be safe about breaking migrations. One approach is to use **RavenJObject** instead of your domain types.

## Thanks

Thanks goes to [Sean Kearon](https://github.com/seankearon) who helped dog food this migration framework and contribute to it.

## Current Version

1.0.0 - Initial Release
