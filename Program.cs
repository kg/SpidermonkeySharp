using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Spidermonkey.Managed;

namespace Test {
    public static class Program {
        public static void Main () {
            /*
            var tc = new Tests();
            tc.MarshalArray();
             */
            using (var ipe = new InProcessEvaluator()) {
                var js = File.ReadAllText(@"C:\Users\Katelyn\Documents\test.js");

                JSError error;
                ipe.Context.Evaluate(ipe.Global, js, out error, filename: "test.js");
                if (error != null)
                    throw error.ToException();
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
        private readonly Queue<string> LoadQueue = new Queue<string>();
        private JSObjectReference OnceLoadedCallback;
        private bool IsLoading = false;

        public InProcessEvaluator () {
            Runtime = new JSRuntime();
            Context = new JSContext(Runtime);
            Request = Context.Request();
            Global = new JSGlobalObject(Context);
            Entry = Context.EnterCompartment(Global);

            if (!Spidermonkey.JSAPI.InitStandardClasses(Context, Global))
                throw new Exception("Failed to initialize standard classes");

            Global.Pointer.DefineFunction(
                Context, "load", (Action<string>)Load
            );
            Global.Pointer.DefineFunction(
                Context, "print", (Action<object>)Print
            );
            Global.Pointer.DefineFunction(
                Context, "putstr", (Action<object>)Putstr
            );
            Global.Pointer.DefineFunction(
                Context, "onceLoaded", (Action<JSObjectReference>)OnceLoaded
            );
        }

        private void OnceLoaded (JSObjectReference callback) {
            OnceLoadedCallback = callback;
        }

        private unsafe void Load (string filename) {
            LoadQueue.Enqueue(filename);

            if (IsLoading)
                return;

            IsLoading = true;

            while (LoadQueue.Count > 0) {
                var _filename = LoadQueue.Dequeue();
                var js = File.ReadAllText(_filename);

                JSError error;
                Context.Evaluate(
                    Global, js,
                    out error,
                    filename: _filename
                );

                Console.WriteLine("// Loaded {0}", _filename);

                if (error != null)
                    throw new Exception("Error while loading " + _filename, error.ToException());
            }

            if (OnceLoadedCallback != null) {
                Spidermonkey.JSObjectPtr zero = Spidermonkey.JSObjectPtr.Zero;
                OnceLoadedCallback.Invoke(&zero);
                OnceLoadedCallback = null;
            }

            IsLoading = false;
        }

        private void Print (object o) {
            Console.WriteLine(Convert.ToString(o));
        }

        private void Putstr (object o) {
            Console.Write(Convert.ToString(o));
        }

        public void Dispose () {
            Entry.Dispose();
            Global.Dispose();
            Request.Dispose();
            Context.Dispose();
            Runtime.Dispose();
        }
    }
}
