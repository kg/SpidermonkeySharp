﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey {
    // FIXME: If the context outlives this instance, the JSClass might get unpinned
    public class JSCustomClass /* : IDisposable */ {
        public readonly JSContextPtr Context;

        private /* readonly */ JSClass ClassDefinition;
        private readonly JSClassPtr ClassPtr;
        private /* readonly */ GCHandle ClassPin;
        private readonly JSNative NativeConstructor;

        private Rooted<JSObjectPtr> PrototypeRoot;

        private readonly JSHandleObject GlobalObject;

        private uint _NumConstructorArguments;
        private JSHandleObject _ParentPrototype = JSHandleObject.Zero;

        public JSCustomClass (JSContextPtr context, string name, JSHandleObject globalObject) {
            Context = context;
            ClassDefinition = new JSClass(name);
            ClassPtr = new JSClassPtr(ref ClassDefinition, out ClassPin);            
            NativeConstructor = Constructor;
            PrototypeRoot = new Rooted<JSObjectPtr>(context);
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

            PrototypeRoot.Value = JSAPI.InitClass(
                Context, GlobalObject, _ParentPrototype,
                ClassPtr, NativeConstructor, _NumConstructorArguments,
                JSPropertySpecPtr.Zero,
                JSFunctionSpecPtr.Zero,
                JSPropertySpecPtr.Zero,
                JSFunctionSpecPtr.Zero
            );
        }

        public bool IsInitialized {
            get {
                return PrototypeRoot.Value.IsNonzero;
            }
        }

        public JSHandleObject PrototypeHandle {
            get {
                return PrototypeRoot;
            }
        }

        public JSObjectPtr Prototype {
            get {
                return PrototypeRoot.Value;
            }
        }

        protected bool Constructor (JSContextPtr context, uint argc, JSCallArgumentsPtr vp) {
            return false;
        }

        /*
        public void Dispose () {
            ClassPin.Free();
        }
         */
    }
}
