using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey {
    [StructLayout(LayoutKind.Sequential)]
    public struct JSErrorReport {
        // FIXME
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct JSClass {
        public static /* readonly */ JSClass DefaultGlobalObjectClass;

        [MarshalAs(UnmanagedType.LPStr)]
        string              name;               
        JSClassFlags        flags;               
                                                 
        // Mandatory function pointer members.
        JSPropertyOp        addProperty;         
        JSDeletePropertyOp  delProperty;         
        JSPropertyOp        getProperty;         
        JSStrictPropertyOp  setProperty;         
        JSEnumerateOp       enumerate;           
        JSResolveOp         resolve;             
        JSConvertOp         convert;

        // FIXME
        /*
        // Optional members (may be null).
        JSFinalizeOp        finalize;            
        JSNative            call;                
        JSHasInstanceOp     hasInstance;         
        JSNative            construct;
        JSTraceOp           trace;
        */

        fixed byte reserved[1024];

        static JSClass () {
            DefaultGlobalObjectClass = new JSClass {
                name = "global",
                flags = JSClassFlags.NEW_RESOLVE | JSClassFlags.IS_GLOBAL,
                addProperty = JSAPI.PropertyStub,
                delProperty = JSAPI.DeletePropertyStub,
                getProperty = JSAPI.PropertyStub,
                setProperty = JSAPI.StrictPropertyStub,
                enumerate   = JSAPI.EnumerateStub,
                resolve     = JSAPI.ResolveStub,
                convert     = JSAPI.ConvertStub,
                /*
                null, null, null, null,
                JS_GlobalObjectTraceHook
                 */
            };
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
}
