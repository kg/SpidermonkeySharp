using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey {
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void JSErrorReporter (
        JSContextPtr cx,
        [MarshalAs(UnmanagedType.LPStr)]
        string message,
        ref JSErrorReport report
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool JSPropertyOp (
        JSContextPtr cx,
        JSHandleObject obj,
        JSHandleId id,
        JSMutableHandleValue vp
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool JSStrictPropertyOp (
        JSContextPtr cx,
        JSHandleObject obj,
        JSHandleId id, 
        bool strict,
        JSMutableHandleValue vp
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool JSDeletePropertyOp (
        JSContextPtr cx,
        JSHandleObject obj,
        JSHandleId id, 
        ref bool succeeded
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool JSEnumerateOp (
        JSContextPtr cx,
        JSHandleObject obj
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool JSResolveOp (
        JSContextPtr cx,
        JSHandleObject obj,
        JSHandleId id
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool JSConvertOp (
        JSContextPtr cx,
        JSHandleObject obj, 
        JSType type, 
        JSMutableHandleValue vp
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool JSFinalizeOp (
        IntPtr fop, JSObjectPtr obj
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool JSHasInstanceOp (
        JSContextPtr cx, JSHandleObject obj, JSMutableHandleValue vp, ref bool bp
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void JSTraceOp (
        IntPtr trc,
        JSObjectPtr obj
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool JSNative (
        JSContextPtr cx, uint argc, JSCallArgs vp
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void JSStringFinalizerCallback (
        IntPtr fin, IntPtr chars
    );
}
