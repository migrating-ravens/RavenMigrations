using Raven.Migrations.Sample.Model;
using System;

namespace Raven.Migrations.Sample.Migrations
{
    /// <summary>
    /// Migration that adds a document.
    /// </summary>
    [Migration(3)]
    public class ReturnOfThePonyExpress : Migration
    {
        public override void Up()
        {
            using var session = this.DocumentStore.OpenSession();
            session.Advanced.WaitForIndexesAfterSaveChanges(TimeSpan.FromSeconds(10));
            session.Store(new Shipper
            {
                Name = "The Pony Express",
                Phone = "What's a phone?"
            });
            session.SaveChanges();
        }
    }
}
