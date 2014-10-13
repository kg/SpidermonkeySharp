using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test {
    public static class Program {
        public static void Main () {
            var tc = new Tests();
            tc.GetPrototypeTest();
            Console.WriteLine("// Press enter");
            Console.ReadLine();
        }
    }
}
