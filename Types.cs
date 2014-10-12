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
        public JSEnumerateOp enumerate;           
        public JSResolveOp resolve;             
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
            enumerate = JSAPI.EnumerateStub;
            resolve = JSAPI.ResolveStub;
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
        bool invisibleToDebugger;
        bool mergeable;
        bool discardSource;
        bool cloneSingletons;
        JSOverrideMode extraWarningsOverride;
        IntPtr zone;
        /* JSTraceOp */ IntPtr traceGlobal;

        // To XDR singletons, we need to ensure that all singletons are all used as
        // templates, by making JSOP_OBJECT return a clone of the JSScript
        // singleton, instead of returning the value which is baked in the JSScript.
        bool singletonsAsTemplates;

        IntPtr addonId;
        bool preserveJitCode;

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
}
