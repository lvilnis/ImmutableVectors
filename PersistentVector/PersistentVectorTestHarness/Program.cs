using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentVectorTestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            var tests = new PersistentVectorTests.PersistentVectorTests();
            tests.RunTests();
        }
    }
}
