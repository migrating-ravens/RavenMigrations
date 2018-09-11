namespace Raven.Migrations.Sample.Migrations
{
    /// <summary>
    /// Migration that adds an .Expendability property to all Employees.
    /// </summary>
    [Migration(1)] // Migrations are run in order. Since we set it to 1, this will run first.
    public class EmployeesHaveExpendability : Migration
    {
        // Do the migration.
        public override void Up()
        {
            // Fire all the doctors! If employee.Notes array contains text with "Ph.D.", the employee is very expendable.
            this.PatchCollection(@"
                from Employees 
                update {
                    var hasPhd = this.Notes.some(n => n.indexOf('Ph.D.') !== -1);
                    this.Expendability = hasPhd ? 'Very' : 'NotSoMuch';
                }
            ");
        }

        // Optional: Undo the migration
        public override void Down()
        {
            this.PatchCollection("from Employees update { delete this.Expendability; }");
        }
    }
}
