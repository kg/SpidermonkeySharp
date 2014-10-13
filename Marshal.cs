using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey.Managed {
    public static class JSMarshal {
        public static object NativeToManaged (JSContextPtr cx, JS.Value value) {
            return value.ToManaged(cx);
        }

        public static JS.Value ManagedToNative (JSContextPtr cx, object value) {
            if (value == null) {
                return JS.Value.Null;
            }

            var s = value as string;
            if (s != null) {
                var pString = JSAPI.NewStringCopy(cx, s);
                return new JS.Value(pString);
            }

            var a = value as Array;
            if (a != null) {
                var va = new JS.ValueArray((uint)a.Length);
                for (int i = 0, l = a.Length; i < l; i++)
                    va.Elements[i] = ManagedToNative(cx, a.GetValue(i));

                JS.ValueArrayPtr vaPtr = va;
                var pArray = JSAPI.NewArrayObject(cx, ref vaPtr);
                return new JS.Value(pArray);
            }

            return (JS.Value)Activator.CreateInstance(typeof(JS.Value), value);
        }

        private static Rooted<JS.Value> NewError (JSContextPtr cx, params object[] errorArguments) {
            var jsErrorArgs = new JS.ValueArray((uint)errorArguments.Length);

            for (int i = 0; i < errorArguments.Length; i++)
                jsErrorArgs.Elements[i] = ManagedToNative(cx, errorArguments[i]);

            JS.ValueArrayPtr vaPtr = jsErrorArgs;
            return new Rooted<JS.Value>(
                cx, new JS.Value(JSAPI.NewError(cx, ref vaPtr))
            );
        }

        public static unsafe Rooted<JS.Value> ManagedToNativeException (JSContextPtr cx, Exception managedException) {
            var errorRoot = NewError(cx, managedException.Message);
            var errorObj = errorRoot.Value.AsObject;
            var pErrorObj = &errorObj;
            var errorObjHandle = new JSHandleObject((IntPtr)pErrorObj);

            var existingStackRoot = new Rooted<JS.Value>(cx);
            JSAPI.GetProperty(
                cx, errorObjHandle, "stack", existingStackRoot
            );
            var existingStackText = existingStackRoot.Value.ToManagedString(cx);

            JSAPI.SetProperty(
                cx, errorObjHandle, "stack",
                new JSString(
                    cx,
                    managedException.StackTrace +
                    "\n//---- JS-to-native boundary ----//\n" +
                    existingStackText
                )
            );

            if (managedException.InnerException != null) {
                var inner = ManagedToNativeException(cx, managedException.InnerException);

                JSAPI.SetProperty(cx, errorObjHandle, "innerException", inner);
            }

            return errorRoot;
        }

        public static void Throw (JSContextPtr cx, Exception managedException) {
            var errorRoot = ManagedToNativeException(cx, managedException);
            JSAPI.SetPendingException(cx, errorRoot);
        }

        public static void Throw (JSContextPtr cx, params object[] errorArguments) {
            var errorRoot = NewError(cx, errorArguments);
            JSAPI.SetPendingException(cx, errorRoot);
        }
    }

}
