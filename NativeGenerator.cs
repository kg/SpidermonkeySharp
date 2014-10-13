using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey.Managed {
    public class NativeToManagedProxy : IDisposable {
        public readonly Delegate ManagedMethod;
        public readonly JSNative WrappedMethod;
        public readonly uint ArgumentCount;
        public readonly ParameterInfo[] ArgumentInfo;
        private readonly GCHandle Pin;

        public NativeToManagedProxy (Delegate managedMethod) {
            ManagedMethod = managedMethod;

            var invoke = GetType().GetMethod("Invoke", BindingFlags.NonPublic | BindingFlags.Instance);
            WrappedMethod = (JSNative)Delegate.CreateDelegate(typeof(JSNative), this, invoke, true);

            ArgumentInfo = managedMethod.Method.GetParameters();
            ArgumentCount = (uint)ArgumentInfo.Length;

            Pin = GCHandle.Alloc(WrappedMethod);
        }

        private static object NativeToManaged (JSContextPtr cx, JS.Value value) {
            return value.ToManaged(cx);
        }

        private static JS.Value ManagedToNative (JSContextPtr cx, object value) {
            if (value == null) {
                return JS.Value.Null;
            }

            var s = value as string;
            if (s != null) {
                var pString = JSAPI.NewStringCopy(cx, s);
                return new JS.Value(pString);
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

        private bool Invoke (JSContextPtr cx, uint argc, JSCallArgumentsPtr args) {
            var managedArgs = new object[ArgumentCount];

            for (uint i = 0, l = Math.Min(ArgumentCount, argc); i < l; i++) {
                try {
                    managedArgs[i] = NativeToManaged(cx, args[i]);
                } catch (Exception exc) {
                    var wrapped = new Exception(
                        "Argument #" + i + " could not be converted",
                        exc
                    );

                    Throw(cx, wrapped);
                    return false;
                }
            }

            object managedResult;
            try {
                managedResult = ManagedMethod.DynamicInvoke(managedArgs);
            } catch (Exception exc) {
                Throw(cx, exc);
                return false;
            }

            JS.Value nativeResult;
            try {
                nativeResult = ManagedToNative(cx, managedResult);
            } catch (Exception exc) {
                var wrapped = new Exception(
                    "Return value could not be converted",
                    exc
                );

                Throw(cx, wrapped);
                return false;
            }

            args.Result = nativeResult;
            return true;
        }

        public void Dispose () {
            Pin.Free();
        }
    }
}
