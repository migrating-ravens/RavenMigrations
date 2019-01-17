using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
