using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JS;

namespace Spidermonkey {
    public unsafe struct JSCallArgs {
        private readonly Value* Values;

        public JSCallArgs (IntPtr ptr) {
            Values = (Value*)ptr.ToPointer();
        }

        public Value Result {
            set {
                Values[0] = value;
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
    }
}
