using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using JS;

namespace Spidermonkey.Managed {
    public class JSObjectReference : IDisposable {
        public readonly JSContextPtr Context;
        public readonly Rooted<JSObjectPtr> Root;

        private Rooted<JS.Value> LazyRootedValue;

        public JSObjectReference(
            JSContextPtr context,
            JSObjectPtr obj
        ) {
            if (context.IsZero)
                throw new ArgumentNullException("context");

            Context = context;
            Root = new Rooted<JSObjectPtr>(Context, obj);
        }

        public JSObjectReference (Rooted<JSObjectPtr> objRoot)
            : this(objRoot.Context, objRoot.Value) {
        }

        /// <summary>
        /// If valueRoot does not contain an object value, this will throw.
        /// </summary>
        public JSObjectReference (Rooted<JS.Value> valueRoot)
            : this(valueRoot.Context, valueRoot.Value.AsObject) {
        }

        /// <summary>
        /// If value does not contain an object, this will throw.
        /// </summary>
        public JSObjectReference (JSContextPtr context, JS.Value value)
            : this(context, value.AsObject) {
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

        public JSObjectReference Prototype {
            get {
                var result = new Rooted<JSObjectPtr>(Context);

                if (!JSAPI.GetPrototype(Context, Root, result))
                    throw new Exception("Failed to get prototype");

                return new JSObjectReference(result);
            }
        }

        public override int GetHashCode () {
            return Pointer.GetHashCode();
        }

        public bool Equals (JSObjectReference rhs) {
            return Pointer.Equals(rhs.Pointer);
        }

        public override bool Equals (object obj) {
            var rhs = obj as JSObjectReference;
            if (rhs != null)
                return Equals(rhs);
            else
                return base.Equals(obj);
        }

        public override string ToString() {
            JS.Value self = this;
            return self.ToManagedString(Context);
        }
    }

    public class JSObjectBuilder : JSObjectReference {
        public static JSObjectPtr CreateInstance (JSContextPtr context) {
            return JSAPI.NewObject(
                context,
                JSClassPtr.Zero,                
                JSHandleObject.Zero, JSHandleObject.Zero
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
            DefaultClassDefinition = new JSClass(
                "global", JSClassFlags.GLOBAL_FLAGS
            );

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
