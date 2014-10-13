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
            if (false) {
                var tc = new Tests();
                tc.ExceptionTest();
            } else {
                using (var ipe = new InProcessEvaluator()) {
                    var js = File.ReadAllText(@"C:\Users\Katelyn\Documents\test.js");

                    JSError error;
                    ipe.Context.Evaluate(ipe.Global, js, out error, filename: "test.js");
                    if (error != null)
                        throw error.ToException();
                }
            }

            Console.WriteLine("// Press enter");
            Console.ReadLine();
        }
    }

    class InProcessEvaluator : IDisposable {
        public readonly JSRuntime Runtime;
        public readonly JSContext Context;
        private readonly JSRequest Request;
        public readonly JSGlobalObject Global;
        private readonly JSCompartmentEntry Entry;
        private int LoadDepth = 0;
        private readonly List<IDisposable> Functions;

        public InProcessEvaluator () {
            Runtime = new JSRuntime(1024 * 1024 * 128);
            Context = new JSContext(Runtime);
            Request = Context.Request();
            Global = new JSGlobalObject(Context);
            Entry = Context.EnterCompartment(Global);

            JSAPI.SetErrorReporter(Context, ReportError);

            if (!JSAPI.InitStandardClasses(Context, Global))
                throw new Exception("Failed to initialize standard classes");

            // The functions we register must be retained so the GC doesn't collect them
            Functions = new List<IDisposable> {
                Global.Pointer.DefineFunction(
                    Context, "load", (Action<string>)Load
                ),
                Global.Pointer.DefineFunction(
                    Context, "print", (Action<object>)Print
                ),
                Global.Pointer.DefineFunction(
                    Context, "putstr", (Action<object>)Putstr
                ),
                Global.Pointer.DefineFunction(
                    Context, "timeout", NoOp
                )
            };
        }

        private JSBool NoOp (JSContextPtr cx, uint argc, JSCallArgumentsPtr argv) {
            argv.Result = JS.Value.Undefined;

            return true;
        }

        private void ReportError (JSContextPtr cx, string message, ref JSErrorReport report) {
            Console.WriteLine(message);
        }

        private unsafe void Load (string filename) {
            try {
                Console.WriteLine("// {0}Loading {1}...", new String(' ', LoadDepth), filename);
                LoadDepth += 1;
                var js = File.ReadAllText(filename);

                JSError error;
                Context.Evaluate(
                    Global, js,
                    out error,
                    filename: filename
                );

                if (error != null) {
                    if (error != null)
                        throw new Exception("Error while loading " + filename, error.ToException());
                } else {
                    Console.WriteLine("// {0}Loaded {1}", new String(' ', LoadDepth - 1), filename);
                }
            } catch (Exception exc) {
                throw;
            } finally {
                LoadDepth -= 1;
            }
        }

        private void Print (object o) {
            Console.WriteLine(Convert.ToString(o));
        }

        private void Putstr (object o) {
            Console.Write(Convert.ToString(o));
        }

        public void Dispose () {
            foreach (var fn in Functions)
                fn.Dispose();

            Entry.Dispose();
            Global.Dispose();
            Request.Dispose();
            Context.Dispose();
            Runtime.Dispose();
        }
    }
}
