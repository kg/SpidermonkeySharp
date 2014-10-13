using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey.Managed {
    // FIXME: If the context outlives this instance, the JSClass might get unpinned
    public class JSCustomClass /* : IDisposable */ {
        public readonly JSContextPtr Context;

        private /* readonly */ JSClass ClassDefinition;
        private readonly JSClassPtr ClassPtr;
        private /* readonly */ GCHandle ClassPin;
        private JSNative NativeConstructor;
        private NativeToManagedProxy ManagedConstructor;

        public JSObjectReference Prototype { get; private set; }
        public JSObjectReference Constructor { get; private set; }

        private readonly JSHandleObject GlobalObject;

        private uint _NumConstructorArguments;
        private JSHandleObject _ParentPrototype = JSHandleObject.Zero;

        public JSCustomClass (JSContextPtr context, string name, JSHandleObject globalObject) {
            Context = context;
            ClassDefinition = new JSClass(name);
            ClassPtr = new JSClassPtr(ref ClassDefinition, out ClassPin);            
            NativeConstructor = DefaultConstructor;
            ManagedConstructor = null;
            GlobalObject = globalObject;
        }

        private void AssertNotInitialized () {
            if (IsInitialized)
                throw new InvalidOperationException("Already initialized");
        }

        public string Name {
            get {
                return ClassDefinition.name;
            }
        }

        public JSClassFlags Flags {
            get {
                return ClassDefinition.flags;
            }
            set {
                AssertNotInitialized();
                ClassDefinition.flags = value;
            }
        }

        public void SetConstructor (JSNative constructor) {
            AssertNotInitialized();
            NativeConstructor = constructor;
            ManagedConstructor = null;
        }

        public void SetConstructor<T> (T constructor) {
            var ctorDelegate = constructor as Delegate;
            if (ctorDelegate == null)
                throw new ArgumentException("Must be a managed delegate", "constructor");

            // No need to create a proxy
            if (ctorDelegate is JSNative) {
                SetConstructor((JSNative)ctorDelegate);
                return;
            }

            AssertNotInitialized();

            // Wrap the delegate in a proxy and retain the proxy
            ManagedConstructor = new NativeToManagedProxy(ctorDelegate);
            NativeConstructor = ManagedConstructor.WrappedMethod;
        }

        public uint NumConstructorArguments {
            get {
                return _NumConstructorArguments;
            }
            set {
                AssertNotInitialized();
                _NumConstructorArguments = value;
            }
        }

        public JSHandleObject ParentPrototype {
            get {
                return _ParentPrototype;
            }
            set {
                AssertNotInitialized();
                _ParentPrototype = value;
            }
        }

        public void Initialize () {
            AssertNotInitialized();

            // Repack in case members were changed
            ClassPtr.Pack(ref ClassDefinition);

            Prototype = new JSObjectReference(Context, JSAPI.InitClass(
                Context, GlobalObject, _ParentPrototype,
                ClassPtr, NativeConstructor, _NumConstructorArguments,
                JSPropertySpecPtr.Zero,
                JSFunctionSpecPtr.Zero,
                JSPropertySpecPtr.Zero,
                JSFunctionSpecPtr.Zero
            ));

            JSObjectPtr ctor = JSAPI.GetConstructor(Context, Prototype);
            Constructor = new JSObjectReference(Context, ctor);
        }

        public bool IsInitialized {
            get {
                return Prototype != null;
            }
        }

        /// <summary>
        /// Creates a this-reference for use inside this class's constructor.
        /// </summary>
        public JS.Value NewObjectForConstructor (uint argc, JSCallArgumentsPtr vp) {
            var callArgs = new JSCallArgs(vp, argc);

            return new JS.Value(
                JSAPI.NewObjectForConstructor(
                    Context, ClassPtr, ref callArgs
                )
            );
        }

        public bool DefaultConstructor (JSContextPtr context, uint argc, JSCallArgumentsPtr vp) {
            // We have to invoke NewObjectForConstructor in order to construct a this-reference
            //  that has our class's prototype and .constructor values.
            vp.Result = NewObjectForConstructor(argc, vp);
            return true;
        }

        /*
        public void Dispose () {
            ClassPin.Free();
        }
         */
    }
}
