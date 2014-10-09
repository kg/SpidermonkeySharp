using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey {
    public struct JSErrorReport {
        // FIXME
    }

    public struct JSObject {
        // FIXME
    }

    public struct jsid {
        // FIXME
    }

    public struct Value {
        // FIXME
    }

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

        fixed byte reserved[64];

        static JSClass () {
            DefaultGlobalObjectClass = new JSClass {
                name = "global",
                flags = JSClassFlags.NEW_RESOLVE | JSClassFlags.IS_GLOBAL,
                addProperty = PropertyStub,
                delProperty = DeletePropertyStub,
                getProperty = PropertyStub,
                setProperty = StrictPropertyStub,
                enumerate   = EnumerateStub,
                resolve     = ResolveStub,
                convert     = ConvertStub,
                /*
                null, null, null, null,
                JS_GlobalObjectTraceHook
                 */
            };
        }

        public static bool PropertyStub(
            JSHandleContext cx, 
            JSHandleObject obj,
            JSHandleId id,
            JSMutableHandleValue vp
        ) {
            return true;
        }

        public static bool StrictPropertyStub(
            JSHandleContext cx,
            JSHandleObject obj,
            JSHandleId id, 
            bool strict,
            JSMutableHandleValue vp
        ) {
            return true;
        }

        public static bool DeletePropertyStub(
            JSHandleContext cx,
            JSHandleObject obj,
            JSHandleId id, 
            ref bool succeeded
        ) {
            succeeded = true;
            return true;
        }

        public static bool EnumerateStub(
            JSHandleContext cx,
            JSHandleObject obj
        ) {
            return true;
        }

        public static bool ResolveStub (
            JSHandleContext cx,
            JSHandleObject obj,
            JSHandleId id
        ) {
            return true;
        }

        public static bool ConvertStub (
            JSHandleContext cx,
            JSHandleObject obj, 
            JSType type, 
            JSMutableHandleValue vp
        ) {
            // FIXME
            /*
            MOZ_ASSERT(type != JSTYPE_OBJECT && type != JSTYPE_FUNCTION);
            MOZ_ASSERT(obj);
            return DefaultValue(cx, obj, type, vp);
             */
            Debugger.Break();
            throw new Exception();
        }    
    }

    public class JSPrincipals {
    }

    public struct JSCompartmentOptions {
        public static /* readonly */ JSCompartmentOptions Default;

        static JSCompartmentOptions () {
            Default = new JSCompartmentOptions {
            };
        }
    }
}
