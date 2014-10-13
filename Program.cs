using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test {
    public static class Program {
        public static void Main () {
            var tc = new Tests();
            tc.WrappedErrorTest();
            Console.WriteLine("// Press enter");
            Console.ReadLine();
        }
    }
}
