using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey {
    public static partial class JSAPI {
        public static readonly bool IsInitialized;

        static JSAPI () {
            IsInitialized = Init();
        }

        public static unsafe bool EvaluateScript (
            JSContextPtr cx,
            JSHandleObject scope,
            string chars,
            string filename,
            uint lineno,
            JSMutableHandleValue rval
        ) {
            fixed (char* pChars = chars)
            fixed (char* pFilename = filename) {
                byte * pFilenameBytes = null;

                if (filename != null) {
                    byte * temp = stackalloc byte[filename.Length + 1];
                    pFilenameBytes = temp;

                    Encoding.ASCII.GetBytes(
                        pFilename, filename.Length,
                        pFilenameBytes, filename.Length
                    );
                }

                return EvaluateUCScript(
                    cx, scope,
                    (IntPtr)pChars, chars.Length,
                    (IntPtr)pFilenameBytes, lineno,
                    rval
                );
            }
        }
    }

    public class JSRuntime {
        public readonly JSRuntimePtr Pointer;

        public JSRuntime (uint maxBytes = 1024 * 1024 * 8) {
            Pointer = JSAPI.NewRuntime(maxBytes);
            if (Pointer.IsZero)
                throw new Exception();
        }

        public static implicit operator JSRuntimePtr (JSRuntime obj) {
            return obj.Pointer;
        }
    }

    public class JSContext {
        public readonly JSContextPtr Pointer;

        public JSContext (JSRuntimePtr runtime) {
            Pointer = JSAPI.NewContext(runtime, 8192);
            if (Pointer.IsZero)
                throw new Exception();
        }

        public JSRequest Request () {
            return new JSRequest(this);
        }

        public JSCompartmentEntry EnterCompartment (JSObjectPtr obj) {
            return new JSCompartmentEntry(this, obj);
        }

        public static implicit operator JSContextPtr (JSContext obj) {
            return obj.Pointer;
        }

        public Rooted<JS.Value> Evaluate (JSHandleObject scope, string scriptSource, string filename = null, uint lineNumber = 0) {
            var resultRoot = new Rooted<JS.Value>(this, JS.Value.Undefined);

            if (JSAPI.EvaluateScript(
                this, scope,
                scriptSource, filename, lineNumber,
                resultRoot
            ))
                return resultRoot;
            else
                return null;
        }
    }

    public struct JSRequest : IDisposable {
        public readonly JSContextPtr Context;

        public JSRequest (JSContextPtr context) {
            Context = context;

            JSAPI.BeginRequest(Context);
        }

        public void Dispose () {
            JSAPI.EndRequest(Context);
        }
    }

    public struct JSCompartmentEntry : IDisposable {
        public readonly JSCompartmentPtr OldCompartment;
        public readonly JSContextPtr Context;

        public JSCompartmentEntry(JSContextPtr context, JSObjectPtr obj) {
            Context = context;
            OldCompartment = JSAPI.EnterCompartment(context, obj);
        }

        public void Dispose () {
            JSAPI.LeaveCompartment(Context, OldCompartment);
        }
    }

    public class JSGlobalObject {
        private static /* readonly */ JSClass DefaultClass;

        public readonly JSContextPtr Context;
        public readonly Rooted<JSObjectPtr> Root;

        static JSGlobalObject () {
            DefaultClass = new JSClass {
                name = Marshal.StringToHGlobalAnsi("global"),
                flags = JSClassFlags.GLOBAL_FLAGS,
                addProperty = JSClass.GetRawFunctionPointer("PropertyStub"),
                delProperty = JSClass.GetRawFunctionPointer("DeletePropertyStub"),
                getProperty = JSClass.GetRawFunctionPointer("PropertyStub"),
                setProperty = JSClass.GetRawFunctionPointer("StrictPropertyStub"),
                enumerate = JSClass.GetRawFunctionPointer("EnumerateStub"),
                resolve = JSClass.GetRawFunctionPointer("ResolveStub"),
                convert = JSClass.GetRawFunctionPointer("ConvertStub"),
                finalize = IntPtr.Zero,
                call = IntPtr.Zero,
                hasInstance = IntPtr.Zero,
                construct = IntPtr.Zero,
                trace = JSClass.GetRawFunctionPointer("GlobalObjectTraceHook")
            };
        }

        public JSGlobalObject (JSContextPtr context) {
            Context = context;
            Root = new Rooted<JSObjectPtr>(Context);

            Root.Value = JSAPI.NewGlobalObject(
                Context,
                ref DefaultClass, null,
                JSOnNewGlobalHookOption.DontFireOnNewGlobalHook,
                ref JSCompartmentOptions.Default
            );
        }

        public static implicit operator JSObjectPtr (JSGlobalObject self) {
            return self.Root.Value;
        }

        public static implicit operator JSHandleObject (JSGlobalObject self) {
            return self.Root;
        }
    }
}
