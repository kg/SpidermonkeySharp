using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey {
    public delegate void JSErrorReporter (
        JSHandleContext cx,
        [MarshalAs(UnmanagedType.LPStr)]
        string message,
        ref JSErrorReport report
    );

    public delegate bool JSPropertyOp(
        JSHandleContext cx,
        JSHandleObject obj,
        JSHandleId id,
        JSMutableHandleValue vp
    );

    public delegate bool JSStrictPropertyOp(
        JSHandleContext cx,
        JSHandleObject obj,
        JSHandleId id, 
        bool strict,
        JSMutableHandleValue vp
    );

    public delegate bool JSDeletePropertyOp(
        JSHandleContext cx,
        JSHandleObject obj,
        JSHandleId id, 
        ref bool succeeded
    );

    public delegate bool JSEnumerateOp(
        JSHandleContext cx,
        JSHandleObject obj
    );

    public delegate bool JSResolveOp (
        JSHandleContext cx,
        JSHandleObject obj,
        JSHandleId id
    );

    public delegate bool JSConvertOp (
        JSHandleContext cx,
        JSHandleObject obj, 
        JSType type, 
        JSMutableHandleValue vp
    );
}
