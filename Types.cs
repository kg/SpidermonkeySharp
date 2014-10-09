using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spidermonkey {
    public delegate void JSErrorReporter (JSContextPtr cx, JSCharPtr message, JSErrorReportPtr report);

    public unsafe struct JSCharPtr {
        public readonly char* Pointer;
    }

    public struct JSErrorReport {
    }

    public unsafe struct JSErrorReportPtr {
        public readonly JSErrorReport* Pointer;
    }

    public struct JSRuntimePtr {
        public readonly IntPtr Pointer;

        public static implicit operator IntPtr(JSRuntimePtr self) {
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
