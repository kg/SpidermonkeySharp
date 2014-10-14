using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey {
    public static partial class JSAPI {
        public static readonly bool IsInitialized;

        public static readonly string DllPath;

        // Unpack our embedded DLLs to a local appdata dir then load spidermonkey from there
        static JSAPI () {
            // FIXME: Two applications running different versions will collide this way :(
            DllPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SpidermonkeySharp"
            );
            Directory.CreateDirectory(DllPath);

            var asm = Assembly.GetExecutingAssembly();
            var expectedTimestamp = File.GetCreationTimeUtc(asm.Location);

            foreach (var name in asm.GetManifestResourceNames()) {
                var filePath = Path.Combine(DllPath, name);
                var fi = new FileInfo(filePath);

                using (var src = asm.GetManifestResourceStream(name)) {
                    // Already unpacked
                    if (
                        fi.Exists &&
                        (fi.CreationTimeUtc == expectedTimestamp) &&
                        (fi.Length == src.Length)
                    )
                        continue;

                    Debug.WriteLine(String.Format("Unpacking '{0}'", filePath));

                    using (var dst = File.Open(filePath, FileMode.Create)) {
                        src.CopyTo(dst);
                    }
                }

                File.SetCreationTimeUtc(filePath, expectedTimestamp);
            }

            // HACK: Set the current directory to the folder where we unpacked our build of SpiderMonkey.
            string priorDirectory = Environment.CurrentDirectory;
            try {
                Environment.CurrentDirectory = DllPath;

                // This call will load the DLLs and initialize the library
                IsInitialized = Init();
            } finally {
                Environment.CurrentDirectory = priorDirectory;
            }
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
                return DefineUCFunction(
                    cx, obj, (IntPtr)pName, (uint)name.Length, call, nargs, attrs
                );
        }

        public static unsafe JSStringPtr NewStringCopy (
            JSContextPtr cx,
            string s
        ) {
            fixed (char* pChars = s)
                return NewUCStringCopyN(cx, (IntPtr)pChars, (uint)s.Length);
        }

        public static unsafe JSObjectPtr NewError (
            JSContextPtr cx,
            ref JS.ValueArrayPtr args
        ) {
            var errorPrototype = GetErrorPrototype(cx);
            var errorConstructor = GetConstructor(cx, &errorPrototype);

            return New(cx, &errorConstructor, ref args);
        }

        public static unsafe bool CompileScript (
            JSContextPtr cx,
            JSHandleObject obj,
            string chars,
            JSCompileOptions options,
            JSMutableHandleScript script
        ) {
            fixed (char* pChars = chars)
                return CompileUCScript(
                    cx, obj, 
                    (IntPtr)pChars, (uint)chars.Length,
                    options, 
                    script
                );
        }

        public static unsafe bool CompileFunction (
            JSContextPtr cx, JSHandleObject obj,
            string name,
            UInt32 nargs, string[] argnames,
            string chars,
            JSCompileOptions options,
            JSMutableHandleFunction fun
        ) {
            if (argnames == null)
                argnames = new string[0];

            if (nargs != argnames.Length)
                throw new ArgumentException("Wrong number of argument names", "nargs");
            char** argNameBuffers = stackalloc char*[argnames.Length];

            for (int i = 0; i < argnames.Length; i++)
                argNameBuffers[i] = (char*)Marshal.StringToHGlobalAnsi(argnames[i]);

            try {
                fixed (char* pChars = chars)
                    return CompileUCFunction(
                        cx, obj,
                        name, nargs, (IntPtr)argNameBuffers,
                        (IntPtr)pChars, (uint)chars.Length,
                        options,
                        fun
                    );
            } finally {
                for (int i = 0; i < argnames.Length; i++)
                    Marshal.FreeHGlobal((IntPtr)argNameBuffers[i]);
            }
        }

        /*
        // HACK: Implement this algorithm by hand since the actual function is broken :/
        public static unsafe bool IsArrayObject (
            JSContextPtr context, JSObjectPtr obj
        ) {
            var proto = JSObjectPtr.Zero;
            if (!GetPrototype(context, &obj, &proto))
                return false;

            var pConstructor = GetConstructor(context, &proto);

            var temp = JS.Value.Undefined;
            var global = GetGlobalForObject(context, obj);
            if (!GetProperty(context, &global, "Array", &temp))
                return false;

            return temp.AsObject.Equals(pConstructor);
        }
         */
    }

    public partial struct JSObjectPtr {
        private unsafe JSHandleObject TransientSelf () {
            fixed (JSObjectPtr * pThis = &this)
                return new JSHandleObject(pThis);
        }

        public Rooted<JS.Value> GetProperty (JSContextPtr context, string name) {
            var result = new Rooted<JS.Value>(context);

            if (JSAPI.GetProperty(context, TransientSelf(), name, result))
                return result;

            result.Dispose();
            return null;
        }

        public bool SetProperty (JSContextPtr context, string name, JSHandleValue value) {
            return JSAPI.SetProperty(context, TransientSelf(), name, value);
        }

        public unsafe bool SetProperty (JSContextPtr context, string name, JSUnrootedValue value) {
            JS.Value _value = value;
            return SetProperty(context, name, &_value);
        }

        /// <summary>
        /// Registers a managed JSNative as a property on the target object.
        /// The JSNative should return true on success and always set a result value.
        /// </summary>
        /// <returns>
        /// A pinning handle for the function that must be retained as long as the function is available to JS.
        /// </returns>
        public unsafe Managed.JSNativePin DefineFunction (
            JSContextPtr context, string name, JSNative call,
            uint nargs = 0, uint attrs = 0
        ) {
            var wrapped = new Managed.JSNativePin(call);
            JSAPI.DefineFunction(
                context, TransientSelf(), name, wrapped.Target, nargs, attrs
            );
            return wrapped;
        }

        /// <summary>
        /// Registers a managed function as a property on the target object.
        /// The managed function is wrapped automatically by a marshalling proxy.
        /// </summary>
        /// <returns>
        /// The function's marshalling proxy that must be retained as long as the function is available to JS.
        /// </returns>
        public Managed.NativeToManagedProxy DefineFunction (
            JSContextPtr context, string name, Delegate @delegate, uint attrs = 0
        ) {
            var wrapped = new Managed.NativeToManagedProxy(@delegate);
            JSAPI.DefineFunction(
                context, TransientSelf(), name, wrapped.WrappedMethod, wrapped.ArgumentCount, attrs
            );
            return wrapped;
        }

        public Rooted<JS.Value> InvokeFunction (
            JSContextPtr context,
            JSHandleObject thisReference,
            params JS.Value[] arguments
        ) {
            var thisValue = new JS.Value(this);
            return thisValue.InvokeFunction(context, thisReference, arguments);
        }

        public unsafe JSObjectPtr InvokeConstructor (
            JSContextPtr context,
            params JS.Value[] arguments
        ) {
            fixed (JSObjectPtr* pThis = &this)
            fixed (JS.Value* pArgs = arguments) {
                var argsPtr = new JS.ValueArrayPtr((uint)arguments.Length, (IntPtr)pArgs);

                return JSAPI.New(context, pThis, ref argsPtr);
            }
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
        internal static unsafe string ToManagedString (JSContextPtr context, JSStringPtr ptr) {
            uint length;

            if (JSAPI.StringHasLatin1Chars(ptr)) {
                var pChars = JSAPI.GetLatin1StringCharsAndLength(
                    context, ref JS.AutoCheckCannotGC.Instance,
                    ptr, out length
                );

                var latin1 = Encoding.GetEncoding("ISO-8859-1");
                return new String((sbyte*)pChars, 0, (int)length, latin1);
            } else {
                var pChars = JSAPI.GetTwoByteStringCharsAndLength(
                    context, ref JS.AutoCheckCannotGC.Instance,
                    ptr, out length
                );

                return new String((char*)pChars, 0, (int)length);
            }
        }

        public unsafe string ToManagedString (JSContextPtr context) {
            return ToManagedString(context, this);
        }

        bool IRootable.AddRoot (JSContextPtr context, JSRootPtr root) {
            return JSAPI.AddStringRoot(context, root);
        }

        void IRootable.RemoveRoot (JSContextPtr context, JSRootPtr root) {
            JSAPI.RemoveStringRoot(context, root);
        }
    }

    public partial struct JSScriptPtr {
        bool IRootable.AddRoot (JSContextPtr context, JSRootPtr root) {
            return JSAPI.AddNamedScriptRoot(context, root, null);
        }

        void IRootable.RemoveRoot (JSContextPtr context, JSRootPtr root) {
            JSAPI.RemoveScriptRoot(context, root);
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
            var root = new Rooted<JS.Value>(Context, JS.Value.Undefined);
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
                Zero = new JSHandleObject(pZero);
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

    public partial struct JSFunctionPtr {
        public Rooted<JS.Value> InvokeFunction (
            JSContextPtr context,
            JSHandleObject thisReference,
            params JS.Value[] arguments
        ) {
            var thisValue = new JS.Value(this);
            return thisValue.InvokeFunction(context, thisReference, arguments);
        }

        public unsafe JSObjectPtr InvokeConstructor (
            JSContextPtr context,
            params JS.Value[] arguments
        ) {
            fixed (JSFunctionPtr* pThis = &this)
            fixed (JS.Value* pArgs = arguments) {
                var argsPtr = new JS.ValueArrayPtr((uint)arguments.Length, (IntPtr)pArgs);

                return JSAPI.New(context, (JSObjectPtr*)pThis, ref argsPtr);
            }
        }
        
        // FIXME: I think this is right since JSFunction derives from JSObject
        bool IRootable.AddRoot (JSContextPtr context, JSRootPtr root) {
            return JSAPI.AddObjectRoot(context, root);
        }

        void IRootable.RemoveRoot (JSContextPtr context, JSRootPtr root) {
            JSAPI.RemoveObjectRoot(context, root);
        }
    }
}
