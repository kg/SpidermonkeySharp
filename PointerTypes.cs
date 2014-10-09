using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey {
    public struct JSRootedObject {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe class _State {
            public readonly IntPtr Pointer;

            public _State (IntPtr pointer) {
                Pointer = pointer;
            }
        }

        public readonly GCHandle Pin;
        public readonly _State State;

        public JSRootedObject (JSHandleContext context, IntPtr pointer) {
            State = new _State(pointer);
            Pin = GCHandle.Alloc(State, GCHandleType.Pinned);

            if (!JSAPI.AddObjectRoot(context, Pin.AddrOfPinnedObject()))
                throw new Exception("Failed to add root");
        }

        public static implicit operator JSHandleObject (JSRootedObject rooted) {
            return (JSHandleObject)rooted.State.Pointer;
        }
    }
}
