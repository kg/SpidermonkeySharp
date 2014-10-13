using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Spidermonkey;

namespace JS {
    public class ValueArray {
        public readonly Value[] Elements;
        private readonly GCHandle ElementsPin;

        public ValueArray (uint length)
            : this(new Value[length]) {
        }

        private ValueArray (Value[] elements) {
            Elements = elements;
            ElementsPin = GCHandle.Alloc(Elements, GCHandleType.Pinned);
        }

        public int Length {
            get {
                return Elements.Length;
            }
        }

        public static implicit operator ValueArrayPtr (ValueArray self) {
            return new ValueArrayPtr(
                (uint)self.Elements.Length,
                self.ElementsPin.AddrOfPinnedObject()
            );
        }
    }

    [StructLayout(LayoutKind.Explicit, Size=8)]
    public struct ValueArrayPtr {
        [FieldOffset(0)]
        public readonly uint Length;
        [FieldOffset(4)]
        public readonly IntPtr Elements;

        public ValueArrayPtr (uint length, IntPtr elements) {
            Length = length;
            Elements = elements;
        }
    }
}