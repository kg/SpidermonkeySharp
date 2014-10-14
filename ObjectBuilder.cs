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
        private List<IDisposable> LazyRetainedObjects;

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
            if (LazyRetainedObjects != null) {
                foreach (var disposable in LazyRetainedObjects)
                    disposable.Dispose();

                LazyRetainedObjects.Clear();
            }

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

        /// <summary>
        /// Recursively retrieves named properties. Returns true if all property names were found.
        /// If a value along the chain is not an object, false is returned and that value is the result.
        /// </summary>
        public bool TryGetNested (out JS.Value result, params string[] propertyNames) {
            var searchScope = Pointer;
            var temp = new Rooted<JS.Value>(Context, JS.Value.Undefined);

            for (int i = 0, l = propertyNames.Length; i < l; i++) {
                var name = propertyNames[i];
                temp = searchScope.GetProperty(Context, name);

                if (i == (l - 1)) {
                    result = temp.Value;
                    return true;
                }

                if (temp.Value.ValueType != JSValueType.OBJECT) {
                    result = temp.Value;
                    return false;
                }

                searchScope = temp.Value.AsObject;
            }

            throw new Exception("Unexpected");
        }

        public bool TryGetNested (out JSObjectPtr result, params string[] propertyNames) {
            JS.Value temp;

            if (TryGetNested(out temp, propertyNames)) {
                if (
                    (temp.ValueType == JSValueType.NULL) ||
                    (temp.ValueType == JSValueType.OBJECT)
                ) {
                    result = temp.AsObject;
                    return true;
                }
            }

            result = default(JSObjectPtr);
            return false;
        }

        public bool TryGetNested (out JSStringPtr result, params string[] propertyNames) {
            JS.Value temp;

            if (TryGetNested(out temp, propertyNames)) {
                if (temp.ValueType == JSValueType.STRING) {
                    result = temp.AsString;
                    return true;
                }
            }

            result = default(JSStringPtr);
            return false;
        }

        /// <summary>
        /// Recursively retrieves named properties. If one of the names is not found, returns null.
        /// </summary>
        public JS.Value? GetNested (params string[] propertyNames) {
            JS.Value result;

            if (TryGetNested(out result, propertyNames))
                return result;
            else
                return null;
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

        public Rooted<JS.Value> Invoke (
            JSHandleObject thisReference,
            params JS.Value[] arguments
        ) {
            return Pointer.InvokeFunction(Context, thisReference, arguments);
        }

        public JSObjectReference Construct (
            params JS.Value[] arguments
        ) {
            return new JSObjectReference(
                Context, Pointer.InvokeConstructor(Context, arguments)
            );
        }

        public void Retain (IDisposable disposable) {
            if (LazyRetainedObjects == null)
                LazyRetainedObjects = new List<IDisposable>();

            LazyRetainedObjects.Add(disposable);
        }

        /// <summary>
        /// Registers a managed JSNative as a property on the target object.
        /// The JSNative should return true on success and always set a result value.
        /// </summary>
        /// <param name="autoRetain">If true, the marshalling proxy is automatically retained by the object wrapper.</param>
        /// <returns>
        /// A pinning handle for the function that must be retained as long as the function is available to JS.
        /// </returns>
        public JSNativePin DefineFunction (
            JSContextPtr context, string name, JSNative call,
            uint nargs = 0, uint attrs = 0,
            bool autoRetain = true
        ) {
            var result = Pointer.DefineFunction(Context, name, @call, nargs, attrs);
            if (autoRetain)
                Retain(result);
            return result;
        }

        /// <summary>
        /// Registers a managed function as a property on the target object.
        /// The managed function is wrapped automatically by a marshalling proxy.
        /// </summary>
        /// <param name="autoRetain">If true, the marshalling proxy is automatically retained by the object wrapper.</param>
        /// <returns>
        /// The function's marshalling proxy that must be retained as long as the function is available to JS.
        /// </returns>
        public NativeToManagedProxy DefineFunction (
            JSContextPtr context, string name, Delegate @delegate, uint attrs = 0,
            bool autoRetain = true
        ) {
            var result = Pointer.DefineFunction(Context, name, @delegate, attrs);
            if (autoRetain)
                Retain(result);
            return result;
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
                "global", JSClassFlags.GLOBAL_FLAGS | JSClassFlags.NEW_RESOLVE
            );

            DefaultClassDefinition.enumerate = (JSEnumerateOp)global_enumerate;
            DefaultClassDefinition.resolve = (JSNewResolveOp)global_resolve;

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

        static unsafe JSBool global_enumerate (JSContextPtr cx, JSHandleObject obj) {
            return JSAPI.EnumerateStandardClasses(cx, obj);
        }

        static unsafe JSBool global_resolve (
            JSContextPtr cx,
            JSHandleObject obj,
            JSHandleId id,
            ref JSObjectPtr objp
        ) {
            JSBool resolved = false;
            objp = JSObjectPtr.Zero;

            if (!JSAPI.ResolveStandardClass(cx, obj, id, ref resolved))
                return false;

            if (resolved) {
                objp = obj.Get();
                return true;
            }

            return true;
        }
    }
}
