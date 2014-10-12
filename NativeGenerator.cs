using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey {
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

        private object NativeToManaged (JSContextPtr cx, JS.Value value) {
            return value.ToManagedValue(cx);
        }

        private JS.Value ManagedToNative (JSContextPtr cx, object value) {
            return (JS.Value)Activator.CreateInstance(typeof(JS.Value), value);
        }

        private bool Invoke (JSContextPtr cx, uint argc, JSCallArgumentsPtr args) {
            var managedArgs = new object[ArgumentCount];
            for (uint i = 0, l = Math.Min(ArgumentCount, argc); i < l; i++)
                managedArgs[i] = NativeToManaged(cx, args[i]);

            var managedResult = ManagedMethod.DynamicInvoke(managedArgs);

            args.Result = ManagedToNative(cx, managedResult);
            return true;
        }

        public void Dispose () {
            Pin.Free();
        }
    }
}
