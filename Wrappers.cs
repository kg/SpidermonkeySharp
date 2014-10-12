using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

    public class JSRuntime : IDisposable {
        public readonly JSRuntimePtr Pointer;
        public bool IsDisposed { get; private set; }

        public JSRuntime (uint maxBytes = 1024 * 1024 * 8) {
            Pointer = JSAPI.NewRuntime(maxBytes);
            if (Pointer.IsZero)
                throw new Exception();
        }

        public static implicit operator JSRuntimePtr (JSRuntime obj) {
            return obj.Pointer;
        }

        public void Dispose () {
            if (IsDisposed)
                return;

            IsDisposed = true;
            GC.SuppressFinalize(this);

            JSAPI.DestroyRuntime(Pointer);
        }

        ~JSRuntime () {
            Dispose();
        }
    }

    public class JSContext : IDisposable {
        private static readonly ConcurrentDictionary<JSContextPtr, WeakReference>
            ContextRegistry = new ConcurrentDictionary<JSContextPtr, WeakReference>(
                JSContextPtr.Comparer
            );

        public readonly JSContextPtr Pointer;
        public readonly JSContextExceptionStatus Exception;
        public bool IsDisposed { get; private set; }

        public readonly WeakReference WeakSelf;

        public JSContext (JSRuntimePtr runtime) {
            Pointer = JSAPI.NewContext(runtime, 8192);
            if (Pointer.IsZero)
                throw new Exception();

            Exception = new JSContextExceptionStatus(Pointer);
            WeakSelf = new WeakReference(this, true);

            if (!ContextRegistry.TryAdd(Pointer, WeakSelf))
                throw new Exception("Failed to update context registry");
        }

        public static JSContext FromPointer (JSContextPtr pointer) {
            WeakReference wr;

            if (!ContextRegistry.TryGetValue(pointer, out wr))
                return null;

            if (!wr.IsAlive)
                return null;

            JSContext result = (JSContext)wr.Target;
            return result;
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

        public void Dispose (bool collectGarbage = true) {
            if (IsDisposed)
                return;

            IsDisposed = true;
            GC.SuppressFinalize(this);

            WeakReference temp;
            if (!ContextRegistry.TryRemove(Pointer, out temp))
                Debug.WriteLine("Failed to remove context from registry");

            if (collectGarbage)
                JSAPI.DestroyContext(Pointer);
            else
                JSAPI.DestroyContextNoGC(Pointer);
        }

        void IDisposable.Dispose () {
            Dispose(true);
        }

        ~JSContext () {
            // Suppress GC since we're in a finalizer.
            Dispose(false);
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

    public partial struct JSObjectPtr {
        private unsafe JSHandleObject TransientSelf () {
            fixed (JSObjectPtr * pThis = &this)
                return new JSHandleObject((IntPtr)pThis);
        }

        public unsafe Rooted<JS.Value> GetProperty (JSContextPtr context, string name) {
            var result = new Rooted<JS.Value>(context);

            if (JSAPI.GetProperty(context, TransientSelf(), name, result))
                return result;

            result.Dispose();
            return null;
        }

        public unsafe bool SetProperty (JSContextPtr context, string name, JSHandleValue value) {
            return JSAPI.SetProperty(context, TransientSelf(), name, value);
        }

        public unsafe bool SetProperty (JSContextPtr context, string name, JSUnrootedValue value) {
            JS.Value _value = value;
            JS.Value* pValue = &_value;
            JSHandleValue handle = new JSHandleValue((IntPtr)pValue);

            return SetProperty(context, name, handle);
        }

        public unsafe JSFunctionPtr DefineFunction (
            JSContextPtr context, string name, JSNative call,
            uint nargs = 0, uint attrs = 0
        ) {
            return JSAPI.DefineFunction(
                context, TransientSelf(), name, call, nargs, attrs
            );
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

        bool IRootable.AddRoot (JSContextPtr context, JSRootPtr root) {
            return JSAPI.AddStringRoot(context, root);
        }

        void IRootable.RemoveRoot (JSContextPtr context, JSRootPtr root) {
            JSAPI.RemoveStringRoot(context, root);
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

    public partial struct JSHandleObject {
        private static JSObjectPtr ZeroPtr = JSObjectPtr.Zero;

        public static readonly JSHandleObject Zero;

        unsafe static JSHandleObject () {
            fixed (JSObjectPtr* pZero = &ZeroPtr)
                Zero = new JSHandleObject((IntPtr)pZero);
        }

        public static JSHandleObject FromValue (Rooted<JS.Value> rval) {
            // Assert that we can actually convert the value into an object pointer.
            var ptr = rval.Value.AsObject;
            // Now get a handle to the value
            JSHandleValue v = rval;
            // HACK: Now take the value handle and turn it into an object handle.
            // This is valid because JS.Value type tagging is at the end of the 8 bytes.
            return new JSHandleObject(v.AddressOfTarget);
        }

        public static explicit operator JSHandleObject (Rooted<JS.Value> rval) {
            return FromValue(rval);
        }
    }
}
