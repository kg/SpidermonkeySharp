using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey {
    public interface IJSPtr {
        JSPtr Pointer { get; }
    }

    public struct JSPtr {
        public readonly IntPtr Raw;

        public static implicit operator IntPtr (JSPtr self) {
            return self.Raw;
        }
    }

    public struct JSHandleObject : IJSPtr {
        public JSPtr Pointer { get; private set; }
    }

    public struct JSHandleId : IJSPtr {
        public JSPtr Pointer { get; private set; }
    }

    public struct JSMutableHandleValue : IJSPtr {
        public JSPtr Pointer { get; private set; }
    }

    public struct JSHandleRuntime : IJSPtr {
        public JSPtr Pointer { get; private set; }
    }

    public struct JSHandleContext : IJSPtr {
        public JSPtr Pointer { get; private set; }
    }

    public struct JSRootedObject {
        public class _State {
            public readonly IntPtr Pointer;

            public _State (IntPtr pointer) {
                Pointer = pointer;
            }
        }

        public readonly GCHandle Pin;
        public readonly _State State;

        public JSRootedObject (JSHandleContext context, IJSPtr pointer) {
            State = new _State(pointer.Pointer);
            Pin = GCHandle.Alloc(State, GCHandleType.Pinned);

            JSAPI.AddObjectRoot(context, Pin.AddrOfPinnedObject());
        }
    }
}
