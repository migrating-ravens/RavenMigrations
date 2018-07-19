using Raven.Migrations.Sample.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            using (var session = this.Db.OpenSession())
            {
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
}
