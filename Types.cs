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
        public IntPtr name;
        public JSClassFlags flags;               
                                                 
        // Mandatory function pointer members.
        public IntPtr addProperty;
        public IntPtr delProperty;
        public IntPtr getProperty;         
        public IntPtr setProperty;         
        public IntPtr enumerate;           
        public IntPtr resolve;             
        public IntPtr convert;

        // Optional members (may be null).
        public IntPtr finalize;            
        public IntPtr call;                
        public IntPtr hasInstance;         
        public IntPtr construct;
        public IntPtr trace;

        fixed byte reserved[10240];

        // FIXME: Can we get rid of this?
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr LoadLibrary (string lpFileName);
        [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern IntPtr GetProcAddress (IntPtr hModule, string procName);

        public static IntPtr GetRawFunctionPointer (string methodName) {
            var t = typeof(JSAPI);
            var m = t.GetMethod(methodName);
            var a = (from attr in m.GetCustomAttributes(false) where attr.GetType().Name.Contains("DllImport") select attr).First();
            var entryPoint = ((DllImportAttribute)a).EntryPoint;
            var dllPath = Environment.CurrentDirectory + "\\mozjs.dll";
            IntPtr hModule = LoadLibrary(dllPath);
            IntPtr pFunction = GetProcAddress(hModule, entryPoint);
            return pFunction;
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

        fixed byte reserved[10240];

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
