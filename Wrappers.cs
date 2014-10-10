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

        public static unsafe bool EvaluateScript(
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

        public static unsafe bool GetProperty (
            JSContextPtr cx,
            JSHandleObject obj,
            string name,
            JSMutableHandleValue vp
        ) {
            fixed (char* pName = name)
                return GetUCProperty(cx, obj, (IntPtr)pName, (uint)name.Length, vp);
        }

        public static unsafe bool SetProperty (
            JSContextPtr cx,
            JSHandleObject obj,
            string name,
            JSHandleValue vp
        ) {
            fixed (char* pName = name)
                return SetUCProperty(cx, obj, (IntPtr)pName, (uint)name.Length, vp);
        }

        public static unsafe JSFunctionPtr DefineFunction (
            JSContextPtr cx,
            JSHandleObject obj,
            string name,
            JSNative call,
            uint nargs, uint attrs
        ) {
            fixed (char* pName = name)
                return JSAPI.DefineUCFunction(
                    cx, obj, (IntPtr)pName, (uint)name.Length, call, nargs, attrs
                );
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
        public readonly JSContextExceptionStatus Exception;

        public JSContext (JSRuntimePtr runtime) {
            Pointer = JSAPI.NewContext(runtime, 8192);
            if (Pointer.IsZero)
                throw new Exception();

            Exception = new JSContextExceptionStatus(Pointer);
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

            resultRoot.Dispose();
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

    public class JSGlobalObject : IDisposable {
        private static /* readonly */ JSClass DefaultClassDefinition;
        private static JSClassPtr DefaultClass;
        private static GCHandle DefaultClassHandle;

        public readonly JSContextPtr Context;
        public readonly Rooted<JSObjectPtr> Root;

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

        public JSGlobalObject (JSContextPtr context) {
            Context = context;
            Root = new Rooted<JSObjectPtr>(Context);

            Root.Value = JSAPI.NewGlobalObject(
                Context,
                DefaultClass, null,
                JSOnNewGlobalHookOption.DontFireOnNewGlobalHook,
                ref JSCompartmentOptions.Default
            );
        }

        public JSObjectPtr Pointer {
            get {
                return Root.Value;
            }
        }

        public void Dispose () {
            Root.Dispose();
        }

        public static implicit operator JSObjectPtr (JSGlobalObject self) {
            return self.Root.Value;
        }

        public static implicit operator JSHandleObject (JSGlobalObject self) {
            return self.Root;
        }
    }

    public partial struct JSObjectPtr {
        public unsafe Rooted<JS.Value> GetProperty (JSContextPtr context, string name) {
            var result = new Rooted<JS.Value>(context);

            fixed (JSObjectPtr * pThis = &this) {
                JSHandleObject handle = new JSHandleObject((IntPtr)pThis);
                if (JSAPI.GetProperty(context, handle, name, result))
                    return result;

                result.Dispose();
                return null;
            }
        }

        public unsafe bool SetProperty (JSContextPtr context, string name, JSHandleValue value) {
            fixed (JSObjectPtr* pThis = &this) {
                JSHandleObject handle = new JSHandleObject((IntPtr)pThis);

                return JSAPI.SetProperty(context, handle, name, value);
            }
        }

        public unsafe bool SetProperty (JSContextPtr context, string name, JS.Value value) {
            JS.Value* pValue = &value;
            JSHandleValue handle = new JSHandleValue((IntPtr)pValue);

            return SetProperty(context, name, handle);
        }

        public unsafe JSFunctionPtr DefineFunction (
            JSContextPtr context, string name, JSNative call,
            uint nargs = 0, uint attrs = 0
        ) {
            fixed (JSObjectPtr* pThis = &this) {
                JSHandleObject handle = new JSHandleObject((IntPtr)pThis);

                return JSAPI.DefineFunction(
                    context, handle, name, call, nargs, attrs
                );
            }
        }

        public unsafe JSFunctionPtr DefineFunction (
            JSContextPtr context, string name, Delegate @delegate, uint attrs = 0
        ) {
            var wrapped = new NativeToManagedProxy(@delegate);
            return DefineFunction(context, name, wrapped.WrappedMethod, wrapped.ArgumentCount, attrs);
        }

        bool IRootable.AddRoot (JSContextPtr context, JSRootPtr root) {
            return JSAPI.AddObjectRoot(context, root);
        }

        void IRootable.RemoveRoot (JSContextPtr context, JSRootPtr root) {
            JSAPI.RemoveObjectRoot(context, root);
        }
    }

    public partial struct JSStringPtr {
        // Creates a copy
        public unsafe string ToManagedString (JSContextPtr context) {
            var length = JSAPI.GetStringLength(this);

            uint numBytes = length * 2;
            var buffer = Marshal.AllocHGlobal((int)numBytes);

            try {
                if (!JSAPI.CopyStringChars(
                    context,
                    new mozilla.Range(buffer, numBytes),
                    this
                ))
                    throw new Exception("String copy failed");

                var result = new String((char*)buffer, 0, (int)length);
                return result;
            } finally {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    public partial struct JSClassPtr {
        public JSClassPtr (ref JSClass value, out GCHandle handle) {
            handle = GCHandle.Alloc(value);
            Pointer = Marshal.AllocHGlobal(Marshal.SizeOf(value));
            Pack(ref value);
        }

        public void Pack (ref JSClass newValue) {
            // FIXME: DeleteOld?
            Marshal.StructureToPtr(newValue, Pointer, false);
        }

        public JSClass Unpack () {
            return (JSClass)Marshal.PtrToStructure(Pointer, typeof(JSClass));
        }
    }

    public class JSContextExceptionStatus {
        public readonly JSContextPtr Context;

        public JSContextExceptionStatus (JSContextPtr context) {
            Context = context;
        }

        public bool IsPending {
            get {
                return JSAPI.IsExceptionPending(Context);
            }
        }

        public Rooted<JS.Value> Get () {
            var root = new Rooted<JS.Value>(Context);
            if (JSAPI.GetPendingException(Context, root))
                return root;

            root.Dispose();
            return null;
        }

        public void Clear () {
            JSAPI.ClearPendingException(Context);
        }
    }
}
