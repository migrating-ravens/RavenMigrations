using RavenMigrations.Migrations;

namespace RavenMigrations.Tests.Migrations.v1
{
    class M_2_FourthTest : CollectionPatchMigration<SampleDoc>
    {
        public override string UpPatch
        {
            get { return "this.Name.Replace('name','patched');"; }
        }
    }
}