using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey {
    public delegate void JSErrorReporter (
        JSContextPtr cx,
        [MarshalAs(UnmanagedType.LPStr)]
        string message,
        ref JSErrorReport report
    );

    public delegate bool JSPropertyOp(
        JSContextPtr cx,
        JSHandleObject obj,
        JSHandleId id,
        JSMutableHandleValue vp
    );

    public delegate bool JSStrictPropertyOp(
        JSContextPtr cx,
        JSHandleObject obj,
        JSHandleId id, 
        bool strict,
        JSMutableHandleValue vp
    );

    public delegate bool JSDeletePropertyOp(
        JSContextPtr cx,
        JSHandleObject obj,
        JSHandleId id, 
        ref bool succeeded
    );

    public delegate bool JSEnumerateOp(
        JSContextPtr cx,
        JSHandleObject obj
    );

    public delegate bool JSResolveOp (
        JSContextPtr cx,
        JSHandleObject obj,
        JSHandleId id
    );

    public delegate bool JSConvertOp (
        JSContextPtr cx,
        JSHandleObject obj, 
        JSType type, 
        JSMutableHandleValue vp
    );
}
