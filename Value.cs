using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Spidermonkey;

namespace JS {
    // HACK: This layout is *only valid* in 32-bit mode
    [StructLayout(LayoutKind.Explicit, Size=8)]
    public struct Value : IRootable {
        public static readonly Value Null = new Value {
            tag = JSValueTag.NULL
        };
        public static readonly Value Undefined = new Value {
            tag = JSValueTag.UNDEFINED
        };

        [FieldOffset(0)]
        UInt64 asBits;

        [FieldOffset(0)]
        Int32 i32;
        [FieldOffset(0)]
        UInt32 u32;
        [FieldOffset(0)]
        double asDouble;
        [FieldOffset(0)]
        JSStringPtr str;
        [FieldOffset(0)]
        JSObjectPtr obj;
        [FieldOffset(0)]
        IntPtr ptr;
        [FieldOffset(0)]
        UIntPtr uintptr;
        // [FieldOffset(0)]
        // JSSymbolPtr sym;

        [FieldOffset(4)]
        JSValueTag tag;

        /*
            js::gc::Cell   *cell;
            JSWhyMagic     why;
            size_t         word;
    } s;
    void *asPtr;
         */

        public JSValueType ValueType {
            get {
                var result = (JSValueType)
                    (tag & ~JSValueTag.CLEAR);

                return result;
            }
        }

        public JSStringPtr? AsString {
            get {
                if (ValueType != JSValueType.STRING)
                    return null;

                return str;
            }
        }

        public unsafe string ToManagedString (JSContextPtr context) {
            fixed (Value * pThis = &this) {
                var handleThis = new JSHandleValue((IntPtr)pThis);

                var resultJsString = JSAPI.ToString(context, handleThis);
                if (resultJsString.IsZero)
                    return null;

                return resultJsString.ToManagedString(context);
            }
        }

        public object ToManagedValue (JSContextPtr context) {
            switch (ValueType) {
                case JSValueType.DOUBLE:
                    return asDouble;
                case JSValueType.INT32:
                    return i32;
                case JSValueType.STRING:
                    return ToManagedString(context);
                case JSValueType.NULL:
                    return null;
                case JSValueType.BOOLEAN:
                    return (i32 != 0);

                default:
                    throw new NotImplementedException("Value type '" + ValueType + "' not convertible");
            }
        }

        bool IRootable.AddRoot (JSContextPtr context, JSRootPtr root) {
            return JSAPI.AddValueRoot(context, root);
        }

        void IRootable.RemoveRoot (JSContextPtr context, JSRootPtr root) {
            JSAPI.RemoveValueRoot(context, root);
        }
    }
}