using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey {
    public class JSObjectReference : IDisposable {
        public readonly JSContextPtr Context;
        public readonly Rooted<JSObjectPtr> Root;

        private Rooted<JS.Value> LazyRootedValue;

        protected JSObjectReference(
            JSContextPtr context,
            JSObjectPtr obj
        ) {
            Context = context;
            Root = new Rooted<JSObjectPtr>(Context, obj);
        }

        public JSObjectPtr Pointer {
            get {
                return Root.Value;
            }
        }

        public void Dispose () {
            Root.Dispose();
        }

        // Since we're rooted it's safe to implicitly convert to a value
        public static implicit operator JS.Value (JSObjectReference self) {
            return new JS.Value(self.Root);
        }

        // We have to lazily construct a rooted JS.Value for ourselves so that
        //  it can serve as the address of our JS.Value in order to produce a JSHandleValue.
        // Gross.
        public static implicit operator JSHandleValue (JSObjectReference self) {
            if (self.LazyRootedValue == null)
                self.LazyRootedValue = new Rooted<JS.Value>(self.Context, self.Root);

            return self.LazyRootedValue;
        }

        public static implicit operator JSObjectPtr (JSObjectReference self) {
            return self.Root.Value;
        }

        public static implicit operator JSHandleObject (JSObjectReference self) {
            return self.Root;
        }

        /// <summary>
        /// Value becomes (or is) rooted by the object.
        /// It's your responsibility to root it if you need it to outlive the object.
        /// </summary>
        public JS.Value this[string name] {
            get {
                return Pointer.GetProperty(Context, name);
            }
            set {
                Pointer.SetProperty(Context, name, value);
            }
        }
    }

    public class JSObjectBuilder : JSObjectReference {
        public static unsafe JSObjectPtr CreateInstance (JSContextPtr context) {
            JSObjectPtr zero;
            var pZero = &zero;
            var hZero = new JSHandleObject((IntPtr)pZero);

            return JSAPI.NewObject(
                context,
                JSClassPtr.Zero,                
                hZero, hZero
            );
        }

        public JSObjectBuilder (JSContextPtr context)
            : base(context, CreateInstance(context)) {
        }
    }

    public class JSGlobalObject : JSObjectReference {
        private static /* readonly */ JSClass DefaultClassDefinition;
        private static JSClassPtr DefaultClass;
        private static GCHandle DefaultClassHandle;

        static JSGlobalObject () {
            DefaultClassDefinition = new JSClass {
                name = "global",
                flags = JSClassFlags.GLOBAL_FLAGS,
                addProperty = JSAPI.PropertyStub,
                delProperty = JSAPI.DeletePropertyStub,
                getProperty = JSAPI.PropertyStub,
                setProperty = JSAPI.StrictPropertyStub,
                enumerate = JSAPI.EnumerateStub,
                resolve = JSAPI.ResolveStub,
                convert = JSAPI.ConvertStub,
                finalize = null,
                call = null,
                hasInstance = null,
                construct = null,
                trace = JSAPI.GlobalObjectTraceHook
            };

            // We have to pin our JSClass (so everything it points to is retained)
            //  and marshal it into a manually-allocated buffer that doesn't expire.
            // JSClass buffer needs to live as long as the global object, or longer.
            DefaultClass = new JSClassPtr(ref DefaultClassDefinition, out DefaultClassHandle);
        }

        public static JSObjectPtr CreateInstance (JSContextPtr context) {
            return JSAPI.NewGlobalObject(
                context,
                DefaultClass, null,
                JSOnNewGlobalHookOption.DontFireOnNewGlobalHook,
                ref JSCompartmentOptions.Default
            );
        }

        public JSGlobalObject (JSContextPtr context)
            : base (context, CreateInstance(context)) {
        }
    }
}
