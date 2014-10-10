using RavenMigrations.Migrations;

namespace RavenMigrations.Tests.Migrations.v1
{
    public class M1_FirstTest : Migration
    {
        public override void Up()
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new SampleDoc { Id = "document/1", Name = "Test name" });
            }
        }
    }
}