using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey {
    public interface IRootable {
        bool AddRoot (JSContextPtr context, JSRootPtr root);
        void RemoveRoot (JSContextPtr context, JSRootPtr root);
    }

    public class Rooted<T> : IDisposable 
        where T : struct, IRootable
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe class _State {
            public T Value;

            public _State (T value) {
                Value = value;
            }
        }

        public readonly JSContextPtr Context;
        public readonly GCHandle Pin;
        public readonly _State State;
        public readonly JSRootPtr Root;
        public bool IsDisposed { get; private set; }

        public Rooted (
            JSContextPtr context, 
            T value = default(T)
        ) {
            Context = context;
            State = new _State(value);
            Pin = GCHandle.Alloc(State, GCHandleType.Pinned);
            Root = new JSRootPtr(Pin.AddrOfPinnedObject());

            if (!default(T).AddRoot(context, Root))
                throw new Exception("Failed to add root");
        }

        public void Dispose () {
            if (IsDisposed)
                return;

            IsDisposed = true;
            default(T).RemoveRoot(Context, Root);
            Pin.Free();
        }

        ~Rooted () {
            Dispose();
        }

        public T Value {
            get {
                return State.Value;
            }
            set {
                State.Value = value;
            }
        }

        public static implicit operator T (Rooted<T> rooted) {
            return rooted.State.Value;
        }
    }
}
