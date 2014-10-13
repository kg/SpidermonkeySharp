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
    public delegate JSBool JSPropertyOp (
        JSContextPtr cx,
        JSHandleObject obj,
        JSHandleId id,
        JSMutableHandleValue vp
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate JSBool JSStrictPropertyOp (
        JSContextPtr cx,
        JSHandleObject obj,
        JSHandleId id, 
        JSBool strict,
        JSMutableHandleValue vp
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate JSBool JSDeletePropertyOp (
        JSContextPtr cx,
        JSHandleObject obj,
        JSHandleId id, 
        ref JSBool succeeded
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate JSBool JSEnumerateOp (
        JSContextPtr cx,
        JSHandleObject obj
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate JSBool JSResolveOp (
        JSContextPtr cx,
        JSHandleObject obj,
        JSHandleId id
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate JSBool JSConvertOp (
        JSContextPtr cx,
        JSHandleObject obj, 
        JSType type, 
        JSMutableHandleValue vp
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate JSBool JSFinalizeOp (
        IntPtr fop, JSObjectPtr obj
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate JSBool JSHasInstanceOp (
        JSContextPtr cx, JSHandleObject obj, JSMutableHandleValue vp, ref JSBool bp
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void JSTraceOp (
        IntPtr trc,
        JSObjectPtr obj
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate JSBool JSNative (
        JSContextPtr cx, uint argc, JSCallArgumentsPtr vp
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void JSStringFinalizerCallback (
        IntPtr fin, IntPtr chars
    );
}
