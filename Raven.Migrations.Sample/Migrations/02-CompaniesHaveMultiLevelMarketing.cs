using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Raven.Migrations.Sample.Migrations
{
    /// <summary>
    /// Patch that adds a property and deletes another.
    /// </summary>
    [Migration(2)]
    public class CompaniesHaveMultiLevelMarketing : Migration
    {
        public override void Up()
        {
            this.PatchCollection(@"
                from Companies as c
                update {
                    c.IsMultiLevelMarketing = Math.random() < .5;
                    delete c.Fax;
                }
            ");
        }
    }
}
