using System;
using System.Runtime.InteropServices;

namespace Spidermonkey {
    public static partial class JSAPI {
        // Automatically invoke JS_Init on first use of API.
        static JSAPI () {
            if (!Init())
                throw new Exception("Failed to initialize JSAPI.");
        }

        /// <summary>
        /// Initialize SpiderMonkey, returning true only if initialization succeeded.  Once this method has succeeded, it is safe to call JS_NewRuntime and other JSAPI methods.
        /// This method must be called before any other JSAPI method is used on any thread.  Once it has been used, it is safe to call any JSAPI method, and it remains safe to do so until JS_ShutDown is correctly called.
        /// It is currently not possible to initialize SpiderMonkey multiple times (that is, calling JS_Init, JSAPI methods, then JS_ShutDown in that order, then doing so again).  This restriction may eventually be lifted.
        /// In the past JS_Init once had the signature JSRuntime * JS_Init(uint32_t maxbytes) and was used to create new JSRuntime instances.  This meaning has been removed; use JS_NewRuntime instead.
        /// </summary>
                [DllImport(
            "mozjs.dll",
            BestFitMapping = false,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "?JS_Init@@YA_NXZ",
            ExactSpelling = true,
            PreserveSig = true,
            SetLastError = false,
            ThrowOnUnmappableChar = true
        )]
                private static extern bool Init ();

        /// <summary>
        /// JS_NewRuntime initializes the JavaScript runtime environment. Call JS_NewRuntime before making any other API calls. JS_NewRuntime allocates memory for the JSRuntime and initializes certain internal runtime structures. maxbytes specifies the number of allocated bytes after which garbage collection is run.
        /// Generally speaking, most applications need only one JSRuntime. In a JS_THREADSAFE build, each runtime is capable of handling multiple execution threads, using one JSContext per thread, sharing the same JSRuntime. You only need multiple runtimes if your application requires completely separate JS engines that cannot share values, objects, and functions.
        /// On success, JS_NewRuntime returns a pointer to the newly created runtime, which the caller must later destroy using JS_DestroyRuntime. Otherwise it returns NULL.
        /// </summary>
                [DllImport(
            "mozjs.dll",
            BestFitMapping = false,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "?JS_NewRuntime@@YAPAUJSRuntime@@IIPAU1@@Z",
            ExactSpelling = true,
            PreserveSig = true,
            SetLastError = false,
            ThrowOnUnmappableChar = true
        )]
                public static extern JSRuntimePtr NewRuntime (
            UInt32 maxBytes,
            UInt32 maxNurseryBytes = DefaultNurseryBytes,
            JSRuntimePtr parentRuntime = default(JSRuntimePtr)
        );

        /// <summary>
        /// JS_NewContext creates a new JSContext in the runtime rt. On success, it returns a pointer to the new context. Otherwise it returns NULL. For more details about contexts, see JSContext. For sample code that creates and initializes a JSContext, see JSAPI User Guide: JSAPI basics.
        /// The stackchunksize parameter does not control the JavaScript stack size. (The JSAPI does not provide a way to adjust the stack depth limit.) Passing a large number for stackchunksize is a mistake. In a DEBUG build, large chunk sizes can degrade performance dramatically. The usual value of 8192 is recommended.
        /// The application must call JS_DestroyContext when it is done using the context. Before a JSRuntime may be destroyed, all the JSContexts associated with it must be destroyed.
        /// The new JSContext initially has no global object.
        /// In a JS_THREADSAFE build, the new JSContext is initially associated with the calling thread. As long as it stays associated with that thread, no other thread may use it or destroy it. A JSContext may be transferred from one thread to another using JS_ClearContextThread and JS_SetContextThread. 
        /// </summary>
                [DllImport(
            "mozjs.dll",
            BestFitMapping = false,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "?JS_NewContext@@YAPAUJSContext@@PAUJSRuntime@@I@Z",
            ExactSpelling = true,
            PreserveSig = true,
            SetLastError = false,
            ThrowOnUnmappableChar = true
        )]
                public static extern JSContextPtr NewContext(JSRuntimePtr rt, UInt32 stackchunksize);

        /// <summary>
        /// JS_SetErrorReporter enables you to define and use your own error reporting mechanism in your applications. The reporter you define is automatically passed a JSErrorReport structure when an error occurs and has been parsed by JS_ReportError. JS_SetErrorReporter returns the previous error reporting function of the context, or NULL if no such function had been set.
        /// Typically, the error reporting mechanism you define should log the error where appropriate (such as to a log file), and display an error to the user of your application. The error you log and display can make use of the information passed about the error condition in the JSErrorReport structure.
        /// The error reporter callback must not reenter the JSAPI. Like all other SpiderMonkey callbacks, the error reporter callback must not throw a C++ exception.
        /// </summary>
                [DllImport(
            "mozjs.dll",
            BestFitMapping = false,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "?JS_SetErrorReporter@@YAP6AXPAUJSContext@@PBDPAUJSErrorReport@@@Z0P6AX012@Z@Z",
            ExactSpelling = true,
            PreserveSig = true,
            SetLastError = false,
            ThrowOnUnmappableChar = true
        )]
                public static extern JSErrorReporter SetErrorReporter(JSContextPtr cx, JSErrorReporter er);

        /// <summary>
        /// JS_NewGlobalObject creates a new global object based on the specified class.
        /// The new object has no parent. It initially has no prototype either, since it is typically the first object created; call JS_InitStandardClasses to create all the standard objects, including Object.prototype, and set the global object's prototype.
        /// The constructor clasp->construct is not called.
        /// On success, JS_NewGlobalObject returns a pointer to the new object. Otherwise it returns NULL.
        /// </summary>
                [DllImport(
            "mozjs.dll",
            BestFitMapping = false,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "?JS_NewGlobalObject@@YAPAVJSObject@@PAUJSContext@@PBUJSClass@@PAUJSPrincipals@@W4OnNewGlobalHookOption@JS@@ABVCompartmentOptions@6@@Z",
            ExactSpelling = true,
            PreserveSig = true,
            SetLastError = false,
            ThrowOnUnmappableChar = true
        )]
                public static extern IntPtr NewGlobalObject(
            JSContextPtr cx, 
            ref JSClass clasp, 
            JSPrincipals principals,
            JSOnNewGlobalHookOption hookOption,
            ref JSCompartmentOptions options
        );

        // TODO: Expose teardown/free APIs
    }
}
