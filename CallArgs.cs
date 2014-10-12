using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JS;

namespace Spidermonkey {
    public unsafe struct JSCallArgumentsPtr {
        private readonly Value* Values;

        public JSCallArgumentsPtr (IntPtr ptr) {
            Values = (Value*)ptr.ToPointer();
        }

        public Value Result {
            set {
                Values[0] = value;
            }
        }

        /// <summary>
        /// WARNING: Setting Result invalidates this!
        /// </summary>
        public Value Callee {
            get {
                return Values[0];
            }
        }

        public Value This {
            get {
                return Values[1];
            }
        }

        public Value this[uint index] {
            get {
                return Values[index + 2];
            }
        }

        public bool IsConstructing {
            get {
                return Values[1].ValueType == JSValueType.MAGIC;
            }
        }

        public static implicit operator Value* (JSCallArgumentsPtr self) {
            return self.Values;
        }
    }

    public unsafe struct JSCallArgs {
        public readonly Value* argv;
        public readonly UInt32 argc;

        public unsafe JSCallArgs (Value* argv, UInt32 argc) {
            this.argv = argv;
            this.argc = argc;
        }

        public JSCallArgs (JSCallArgumentsPtr argv, UInt32 argc) {
            this.argv = argv;
            this.argc = argc;
        }
    }
}
