using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spidermonkey {
    public struct JSHandleObject {
        public readonly IntPtr Pointer;

        public static implicit operator IntPtr (JSHandleObject self) {
            return self.Pointer;
        }
    }

    public struct JSHandleId {
        public readonly IntPtr Pointer;

        public static implicit operator IntPtr (JSHandleId self) {
            return self.Pointer;
        }
    }

    public struct JSMutableHandleValue {
        public readonly IntPtr Pointer;

        public static implicit operator IntPtr (JSMutableHandleValue self) {
            return self.Pointer;
        }
    }

    public struct JSRuntimePtr {
        public readonly IntPtr Pointer;

        public static implicit operator IntPtr (JSRuntimePtr self) {
            return self.Pointer;
        }
    }

    public struct JSContextPtr {
        public readonly IntPtr Pointer;

        public static implicit operator IntPtr (JSContextPtr self) {
            return self.Pointer;
        }
    }
}
