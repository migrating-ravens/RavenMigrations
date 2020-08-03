using Raven.Migrations.Sample.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Raven.Migrations.Sample.Migrations
{
    /// <summary>
    /// This migration shows that you can rely on dependency injected (DI) services in your migration.
    /// </summary>
    [Migration(4)]
    public class MigrationsCanRelyOnDIServices : Migration
    {
        private readonly ISillyService thing;

        /// <summary>
        /// Here's our constructor, which expects an injected service. This will be injected for us when migrations are run.
        /// </summary>
        /// <param name="thing"></param>
        public MigrationsCanRelyOnDIServices(ISillyService thing)
        {
            this.thing = thing;
        }

        public override void Up()
        {
            // Use our dependency injected service.
            this.thing.DoSomething();

            // Run the rest of our patch.
            this.PatchCollection(@"
                from Employees
                update {
                    this.IsSilly = true;
                }
            ");
        }
    }
}
