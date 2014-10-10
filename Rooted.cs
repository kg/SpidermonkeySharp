using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey {
    // FIXME: Make this a class to disable default constructor?
    public struct Rooted<T> : IDisposable 
        where T : struct
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe class _State {
            public T Value;

            public _State (T value) {
                Value = value;
            }
        }

        public readonly GCHandle Pin;
        public readonly _State State;

        public Rooted (
            JSContextPtr context, 
            T value = default(T)
        ) {
            State = new _State(value);
            Pin = GCHandle.Alloc(State, GCHandleType.Pinned);

            if (!JSAPI.AddObjectRoot(context, this))
                throw new Exception("Failed to add root");
        }

        public void Dispose () {
            Pin.Free();
        }

        public T Value {
            get {
                return State.Value;
            }
        }

        public static implicit operator T (Rooted<T> rooted) {
            return rooted.State.Value;
        }

        // Treats the address of our pinned object pointer as a Handle<JSObject *>.
        // Make sure the root doesn't die while this handle value is alive!
        public static unsafe implicit operator JSRootPtr (Rooted<T> rooted) {
            return new JSRootPtr(rooted.Pin.AddrOfPinnedObject());
        }
    }
}
