using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey.JS {
    [StructLayout(LayoutKind.Sequential)]
    public struct Value {
        UInt64 asBits;
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
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr LoadLibrary (string lpFileName);
        [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern IntPtr GetProcAddress (IntPtr hModule, string procName);

        public static /* readonly */ JSClass DefaultGlobalObjectClass;

        // char *
        IntPtr              name;               
        JSClassFlags        flags;               
                                                 
        // Mandatory function pointer members.
        IntPtr              addProperty;         
        IntPtr              delProperty;         
        IntPtr              getProperty;         
        IntPtr              setProperty;         
        IntPtr              enumerate;           
        IntPtr              resolve;             
        IntPtr              convert;

        // Optional members (may be null).
        IntPtr              finalize;            
        IntPtr              call;                
        IntPtr              hasInstance;         
        IntPtr              construct;
        IntPtr              trace;

        fixed byte reserved[10240];

        static IntPtr GetRawFunctionPointer(string methodName) {
            var t = typeof(JSAPI);
            var m = t.GetMethod(methodName);
            var a = (from attr in m.GetCustomAttributes(false) where attr.GetType().Name.Contains("DllImport") select attr).First();
            var entryPoint = ((DllImportAttribute)a).EntryPoint;
            var dllPath = Environment.CurrentDirectory + "\\mozjs.dll";
            IntPtr hModule = LoadLibrary(dllPath);
            IntPtr pFunction = GetProcAddress(hModule, entryPoint);
            return pFunction;
        }

        static JSClass () {
            DefaultGlobalObjectClass = new JSClass {
                name = Marshal.StringToHGlobalAnsi("global"),
                flags = JSClassFlags.GLOBAL_FLAGS,
                addProperty = GetRawFunctionPointer("PropertyStub"),
                delProperty = GetRawFunctionPointer("DeletePropertyStub"),
                getProperty = GetRawFunctionPointer("PropertyStub"),
                setProperty = GetRawFunctionPointer("StrictPropertyStub"),
                enumerate   = GetRawFunctionPointer("EnumerateStub"),
                resolve     = GetRawFunctionPointer("ResolveStub"),
                convert     = GetRawFunctionPointer("ConvertStub"),
                finalize    = IntPtr.Zero,
                call        = IntPtr.Zero,
                hasInstance = IntPtr.Zero,
                construct   = IntPtr.Zero,
                trace       = GetRawFunctionPointer("GlobalObjectTraceHook")
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
