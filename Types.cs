using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace mozilla {
    [StructLayout(LayoutKind.Sequential)]
    public struct Range {
        readonly IntPtr mStart, mEnd;

        public Range (IntPtr start, IntPtr end) {
            mStart = start;
            mEnd = end;
        }

        public Range (IntPtr start, uint length) {
            mStart = start;
            mEnd = new IntPtr(start.ToInt64() + length);
        }
    }
}

namespace Spidermonkey {
    [StructLayout(LayoutKind.Sequential)]
    public struct jsid {
        UInt32 asBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JSErrorReport {
        // FIXME
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct JSClass {
        // char *
        [MarshalAs(UnmanagedType.LPStr)]
        public string name;
        public JSClassFlags flags;

        // FIXME: These delegates will point to stubs for the
        //  actual functions, which means if you do:
        // addProperty = JSAPI.PropertyStub
        // It will point to a forwarder and not the actual function.
        // In practice this should be OK, but it might be a problem...

        // Mandatory function pointer members.
        public JSPropertyOp addProperty;
        public JSDeletePropertyOp delProperty;
        public JSPropertyOp getProperty;         
        public JSStrictPropertyOp setProperty;         
        public Delegate enumerate;           
        // JSResolveOp or JSNewResolveOp
        public Delegate resolve;
        public JSConvertOp convert;

        // Optional members (may be null).
        public JSFinalizeOp finalize;            
        public JSNative call;                
        public JSHasInstanceOp hasInstance;         
        public JSNative construct;
        public JSTraceOp trace;

        // HACK
        fixed byte reserved[1024];

        public JSClass (string name, JSClassFlags flags = default(JSClassFlags)) {
            this = default(JSClass);

            this.name = name;
            this.flags = flags;

            addProperty = JSAPI.PropertyStub;
            delProperty = JSAPI.DeletePropertyStub;
            getProperty = JSAPI.PropertyStub;
            setProperty = JSAPI.StrictPropertyStub;
            enumerate = (JSEnumerateOp)JSAPI.EnumerateStub;
            resolve = (JSResolveOp)JSAPI.ResolveStub;
            convert = JSAPI.ConvertStub;
            trace = JSAPI.GlobalObjectTraceHook;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class JSPrincipals {
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct JSCompartmentOptions {
        public static /* readonly */ JSCompartmentOptions Default;

        JSVersion version;
        JSBool invisibleToDebugger;
        JSBool mergeable;
        JSBool discardSource;
        JSBool cloneSingletons;
        JSOverrideMode extraWarningsOverride;
        IntPtr zone;
        /* JSTraceOp */ IntPtr traceGlobal;

        // To XDR singletons, we need to ensure that all singletons are all used as
        // templates, by making JSOP_OBJECT return a clone of the JSScript
        // singleton, instead of returning the value which is baked in the JSScript.
        JSBool singletonsAsTemplates;

        IntPtr addonId;
        JSBool preserveJitCode;

        // HACK
        fixed byte reserved[1024];

        static JSCompartmentOptions () {
            Default = new JSCompartmentOptions {
                version = JSVersion.JSVERSION_LATEST,
                invisibleToDebugger = false,
                mergeable = false,
                discardSource = false,
                cloneSingletons = false,
                singletonsAsTemplates = true,
                preserveJitCode = false
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JSContextOptions {
        public JSContextOptionFlags Options;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe class JSCompileOptions {
        public static JSCompileOptions Default;


        unsafe delegate JSObjectPtr GetElementDelegate();
        unsafe delegate JSStringPtr GetStringDelegate();
        unsafe delegate JSScriptPtr GetScriptDelegate();

        // Static so the delegate instances are retained by the GC
        static GetElementDelegate GetElement;
        static GetStringDelegate GetElementAttributeName;
        static GetScriptDelegate GetIntroductionScript;

        private static JSObjectPtr getElement () {
            return JSObjectPtr.Zero;
        }

        private static JSStringPtr getElementAttributeName () {
            return JSStringPtr.Zero;
        }

        private static JSScriptPtr getIntroductionScript () {
            return JSScriptPtr.Zero;
        }


        [StructLayout(LayoutKind.Sequential)]
        unsafe struct VTable {
            public IntPtr element;
            public IntPtr elementAttributeName;
            public IntPtr introductionScript;
        }

        private static VTable DefaultVTable;
        private static IntPtr pDefaultVTable;

        private IntPtr pVTable;

        public JSBool mutedErrors;

        [MarshalAs(UnmanagedType.LPStr)]
        public string filename;

        [MarshalAs(UnmanagedType.LPStr)]
        public string introducerFilename;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string sourceMapURL;

        // POD options.
        public JSVersion version;
        public JSBool versionSet;
        public JSBool utf8;
        public UInt32 lineno;
        public UInt32 column;
        public JSBool compileAndGo;
        public JSBool forEval;
        public JSBool defineOnScope;
        public JSBool noScriptRval;
        public JSBool selfHostingMode;
        public JSBool canLazilyParse;
        public JSBool strictOption;
        public JSBool extraWarningsOption;
        public JSBool werrorOption;
        public JSBool asmJSOption;
        public JSBool forceAsync;
        public JSBool installedFile;  // 'true' iff pre-compiling js file in packaged app
        public JSBool sourceIsLazy;

        // |introductionType| is a statically allocated C string:
        // one of "eval", "Function", or "GeneratorFunction".
        [MarshalAs(UnmanagedType.LPStr)]
        string introductionType;

        UInt32 introductionLineno;
        UInt32 introductionOffset;
        JSBool hasIntroductionInfo;


        static JSCompileOptions () {
            GetElement = getElement;
            GetElementAttributeName = getElementAttributeName;
            GetIntroductionScript = getIntroductionScript;

            DefaultVTable = new VTable {
                element = Marshal.GetFunctionPointerForDelegate(GetElement),
                elementAttributeName = Marshal.GetFunctionPointerForDelegate(GetElementAttributeName),
                introductionScript = Marshal.GetFunctionPointerForDelegate(GetIntroductionScript),
            };

            pDefaultVTable = Marshal.AllocHGlobal(Marshal.SizeOf(DefaultVTable));
            Marshal.StructureToPtr(DefaultVTable, pDefaultVTable, false);

            Default = new JSCompileOptions();
        }


        public unsafe JSCompileOptions () {
            pVTable = pDefaultVTable;
            mutedErrors = false;
            filename = null;
            introducerFilename = null;
            sourceMapURL = null;
            version = JSVersion.JSVERSION_UNKNOWN;
            versionSet = false;
            utf8 = false;
            lineno = 1;
            column = 0;
            compileAndGo = false;
            forEval = false;
            defineOnScope = true;
            noScriptRval = false;
            selfHostingMode = false;
            canLazilyParse = true;
            strictOption = false;
            extraWarningsOption = false;
            werrorOption = false;
            asmJSOption = false;
            forceAsync = false;
            installedFile = false;
            sourceIsLazy = false;
            introductionType = null;
            introductionLineno = 0;
            introductionOffset = 0;
            hasIntroductionInfo = false;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JSBool {
        private readonly byte _Value;

        public JSBool (byte value) {
            _Value = value;
        }

        public JSBool (bool value) {
            _Value = (byte)(value ? 1 : 0);
        }

        public bool Value {
            get {
                return _Value != 0;
            }
        }

        public static implicit operator bool (JSBool b) {
            return (b._Value != 0);
        }

        public static implicit operator JSBool (bool b) {
            return new JSBool(b);
        }

        public static implicit operator JSBool (byte b) {
            return new JSBool(b);
        }
    }
}
