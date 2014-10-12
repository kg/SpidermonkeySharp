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
        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct Packed {
            [FieldOffset(0)]
            public Int32 i32;
            [FieldOffset(0)]
            public UInt32 u32;
            [FieldOffset(0)]
            public double f64;
            [FieldOffset(0)]
            public JSStringPtr str;
            [FieldOffset(0)]
            public JSObjectPtr obj;
            [FieldOffset(0)]
            public IntPtr ptr;
            [FieldOffset(0)]
            public UIntPtr uintptr;
        }

        public static readonly Value Null = new Value {
            tag = JSValueTag.NULL
        };
        public static readonly Value Undefined = new Value {
            tag = JSValueTag.UNDEFINED
        };

        [FieldOffset(0)]
        UInt64 asBits;
        [FieldOffset(0)]
        Packed packed;

        [FieldOffset(4)]
        JSValueTag tag;

        /*
            js::gc::Cell   *cell;
            JSWhyMagic     why;
            size_t         word;
    } s;
    void *asPtr;
         */

        public Value (bool b) {
            this = default(Value);
            tag = JSValueTag.BOOLEAN;
            packed.i32 = b ? 1 : 0;
        }

        public Value (int i) {
            this = default(Value);
            tag = JSValueTag.INT32;
            packed.i32 = i;
        }

        public Value (double d) {
            this = default(Value);
            tag = JSValueTag.DOUBLE;
            packed.f64 = d;
        }

        /// <summary>
        /// WARNING: Make sure this object is already rooted!
        /// </summary>
        /// <param name="o"></param>
        public Value (JSObjectPtr o) {
            this = default(Value);

            if (o.IsZero) {
                tag = JSValueTag.NULL;
            } else {
                tag = JSValueTag.OBJECT;
            }

            packed.obj = o;
        }

        /// <summary>
        /// WARNING: Make sure this string is already rooted!
        /// </summary>
        /// <param name="s"></param>
        public Value (JSStringPtr s) {
            this = default(Value);
            tag = JSValueTag.STRING;
            packed.str = s;
        }

        public bool IsNullOrUndefined {
            get {
                return (ValueType == JSValueType.NULL) ||
                    (ValueType == JSValueType.UNDEFINED);
            }
        }

        public JSValueType ValueType {
            get {
                var result = (JSValueType)
                    (tag & ~JSValueTag.CLEAR);

                return result;
            }
            set {
                var newTag = (tag & JSValueTag.CLEAR) | (JSValueTag)value;
                tag = newTag;
            }
        }

        public JSStringPtr AsString {
            get {
                if (ValueType != JSValueType.STRING)
                    throw new InvalidOperationException("Value is not a string");

                return packed.str;
            }
        }

        public JSObjectPtr AsObject {
            get {
                if (ValueType == JSValueType.NULL)
                    return JSObjectPtr.Zero;
                else if (ValueType == JSValueType.OBJECT)
                    return packed.obj;
                else
                    throw new InvalidOperationException("Value is not an object");
            }
        }

        public unsafe JSType GetJSType (JSContextPtr context) {
            fixed (Value * pThis = &this)
                return JSAPI.TypeOfValue(context, new JSHandleValue((IntPtr)pThis));
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
                    return packed.f64;
                case JSValueType.INT32:
                    return packed.i32;
                case JSValueType.STRING:
                    return ToManagedString(context);
                case JSValueType.NULL:
                    return null;
                case JSValueType.BOOLEAN:
                    return (packed.i32 != 0);

                default:
                    throw new NotImplementedException("Value type '" + ValueType + "' not convertible");
            }
        }

        public static explicit operator double (Value value) {
            switch (value.ValueType) {
                case JSValueType.DOUBLE:
                    return value.packed.f64;

                case JSValueType.INT32:
                case JSValueType.BOOLEAN:
                    return value.packed.i32;

                default:
                    throw new InvalidOperationException("Value is not numeric");
            }
        }

        public static explicit operator Int32 (Value value) {
            switch (value.ValueType) {
                case JSValueType.INT32:
                case JSValueType.BOOLEAN:
                    return value.packed.i32;

                default:
                    throw new InvalidOperationException("Value is not integral");
            }
        }

        public unsafe Rooted<Value> InvokeFunction (
            JSContextPtr context,
            JSHandleObject thisReference,
            params Value[] arguments
        ) {
            fixed (Value * pThis = &this)
            fixed (Value * pArgs = arguments) {
                var argsPtr = new ValueArrayPtr((uint)arguments.Length, (IntPtr)pArgs);
                var resultRoot = new Rooted<Value>(context);
                var thisVal = new JSHandleValue((IntPtr)pThis);

                if (JSAPI.CallFunctionValue(
                    context, thisReference,
                    thisVal,
                    ref argsPtr,
                    resultRoot
                ))
                    return resultRoot;

                resultRoot.Dispose();
                return null;
            }
        }

        public JSObjectPtr InvokeConstructor (
            JSContextPtr context,
            params Value[] arguments
        ) {
            var obj = AsObject;
            return obj.InvokeConstructor(context, arguments);
        }

        public static explicit operator JSObjectPtr (Value value) {
            return value.AsObject;
        }

        public static explicit operator JSStringPtr (Value value) {
            return value.AsString;
        }

        // Allow implicit conversions from rooted JSObject to Value
        public static implicit operator Value (Rooted<JSObjectPtr> obj) {
            return new Value(obj);
        }

        public static implicit operator Value (Rooted<JSStringPtr> str) {
            return new Value(str);
        }

        bool IRootable.AddRoot (JSContextPtr context, JSRootPtr root) {
            return JSAPI.AddValueRoot(context, root);
        }

        void IRootable.RemoveRoot (JSContextPtr context, JSRootPtr root) {
            JSAPI.RemoveValueRoot(context, root);
        }
    }

    public struct AutoCheckCannotGC {
        public static AutoCheckCannotGC Instance = new AutoCheckCannotGC();
    }
}

namespace Spidermonkey {
    // Type system ambiguity buster
    public struct JSUnrootedValue {
        public readonly JS.Value Value;

        public JSUnrootedValue (JS.Value value) {
            Value = value;
        }

        public static implicit operator JS.Value (JSUnrootedValue self) {
            return self.Value;
        }

        public static implicit operator JSUnrootedValue (JS.Value val) {
            return new JSUnrootedValue(val);
        }
    }
}