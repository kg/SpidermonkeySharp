using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Spidermonkey;
using Spidermonkey.Managed;

namespace Test {
    public static class Program {
        public static void Main () {
            var tc = new Tests();
            tc.CompileThenExecute();

            Console.WriteLine("// Press enter");
            Console.ReadLine();
        }
    }
}
