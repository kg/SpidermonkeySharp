using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Spidermonkey.Managed;

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

        // Bound via reflection
        private JSBool Invoke (JSContextPtr cx, uint argc, JSCallArgumentsPtr args) {
            var managedArgs = new object[ArgumentCount];

            for (uint i = 0, l = Math.Min(ArgumentCount, argc); i < l; i++) {
                try {
                    managedArgs[i] = JSMarshal.NativeToManaged(cx, args[i]);
                } catch (Exception exc) {
                    var wrapped = new Exception(
                        "Argument #" + i + " could not be converted",
                        exc
                    );

                    JSMarshal.Throw(cx, wrapped);
                    return false;
                }
            }

            object managedResult;
            try {
                managedResult = ManagedMethod.DynamicInvoke(managedArgs);
            } catch (Exception exc) {
                JSMarshal.Throw(cx, exc);
                return false;
            }

            if (ManagedMethod.Method.ReturnType.FullName == "System.Void") {
                args.Result = JS.Value.Undefined;
                return true;
            }

            JS.Value nativeResult;
            try {
                nativeResult = JSMarshal.ManagedToNative(cx, managedResult);
            } catch (Exception exc) {
                var wrapped = new Exception(
                    "Return value could not be converted",
                    exc
                );

                JSMarshal.Throw(cx, wrapped);
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
