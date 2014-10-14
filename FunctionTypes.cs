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

    // The old-style JSClass.enumerate op should define all lazy properties not
    // yet reflected in obj.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate JSBool JSEnumerateOp (
        JSContextPtr cx,
        JSHandleObject obj
    );

    // This function type is used for callbacks that enumerate the properties of
    // a JSObject.  The behavior depends on the value of enum_op:
    //
    //  JSENUMERATE_INIT
    //    A new, opaque iterator state should be allocated and stored in *statep.
    //    (You can use PRIVATE_TO_JSVAL() to tag the pointer to be stored).
    //
    //    The number of properties that will be enumerated should be returned as
    //    an integer jsval in *idp, if idp is non-null, and provided the number of
    //    enumerable properties is known.  If idp is non-null and the number of
    //    enumerable properties can't be computed in advance, *idp should be set
    //    to JSVAL_ZERO.
    //
    //  JSENUMERATE_INIT_ALL
    //    Used identically to JSENUMERATE_INIT, but exposes all properties of the
    //    object regardless of enumerability.
    //
    //  JSENUMERATE_NEXT
    //    A previously allocated opaque iterator state is passed in via statep.
    //    Return the next jsid in the iteration using *idp.  The opaque iterator
    //    state pointed at by statep is destroyed and *statep is set to JSVAL_NULL
    //    if there are no properties left to enumerate.
    //
    //  JSENUMERATE_DESTROY
    //    Destroy the opaque iterator state previously allocated in *statep by a
    //    call to this function when enum_op was JSENUMERATE_INIT or
    //    JSENUMERATE_INIT_ALL.
    //
    // The return value is used to indicate success, with a value of false
    // indicating failure.
    /* FIXME
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate JSBool JSNewEnumerateOp (
        JSContextPtr cx, JSHandleObject obj, JSIterateOp enum_op,
        JSMutableHandleValue statep, JSMutableHandleId idp
    );
     */

    // Resolve a lazy property named by id in obj by defining it directly in obj.
    // Lazy properties are those reflected from some peer native property space
    // (e.g., the DOM attributes for a given node reflected as obj) on demand.
    //
    // JS looks for a property in an object, and if not found, tries to resolve
    // the given id.  If resolve succeeds, the engine looks again in case resolve
    // defined obj[id].  If no such property exists directly in obj, the process
    // is repeated with obj's prototype, etc.
    //
    // NB: JSNewResolveOp provides a cheaper way to resolve lazy properties.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate JSBool JSResolveOp (
        JSContextPtr cx,
        JSHandleObject obj,
        JSHandleId id
    );

    // Like JSResolveOp, except the *objp out parameter, on success, should be null
    // to indicate that id was not resolved; and non-null, referring to obj or one
    // of its prototypes, if id was resolved.  The hook may assume *objp is null on
    // entry.
    //
    // This hook instead of JSResolveOp is called via the JSClass.resolve member
    // if JSCLASS_NEW_RESOLVE is set in JSClass.flags.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate JSBool JSNewResolveOp (
        JSContextPtr cx,
        JSHandleObject obj,
        JSHandleId id,
        ref JSObjectPtr objp
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
