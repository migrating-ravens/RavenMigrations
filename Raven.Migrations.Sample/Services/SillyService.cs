using System;

namespace Raven.Migrations.Sample.Services
{
    public interface ISillyService
    {
        void DoSomething();
    }

    public class SillyService : ISillyService
    {
        public void DoSomething()
        {
            Console.WriteLine("2 + 2 is {0}", 2 + 2);
        }
    }
}
