using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spidermonkey.Managed {
    public class JSArray : JSObjectReference {
        public JSArray(
            JSContextPtr context,
            JSObjectPtr obj
        ) : base (context, obj) {
            if (!JSAPI.IsArrayObject(context, Root))
                throw new ArgumentException("Value is not an array", "obj");
        }

        public JSArray (Rooted<JSObjectPtr> objRoot)
            : this(objRoot.Context, objRoot.Value) {
        }

        /// <summary>
        /// If valueRoot does not contain an object value, this will throw.
        /// </summary>
        public JSArray (Rooted<JS.Value> valueRoot)
            : this(valueRoot.Context, valueRoot.Value.AsObject) {
        }

        public JSArray (JSContextPtr context, uint length)
            : this(context, JSAPI.NewArrayObject(context, length)) {
        }

        public JSArray (JSContextPtr context, ref JS.ValueArrayPtr contents)
            : this(context, JSAPI.NewArrayObject(context, ref contents)) {
        }

        public JSArray (JSContextPtr context, JS.Value[] contents, uint offset = 0, uint count = 0xFFFFFFFF)
            : this(context, CreateArrayImpl(context, contents, offset, count)) {
        }

        private unsafe static JSObjectPtr CreateArrayImpl(
            JSContextPtr context, JS.Value[] contents, uint offset, uint count
        ) {
            if (count == 0xFFFFFFFF)
                count = (uint)(contents.Length - offset);

            if (
                (count > (contents.Length - offset)) ||
                (offset >= contents.Length)
            )
                throw new ArgumentException("offset/count out of range");
            
            fixed (JS.Value * pContents = &contents[offset]) {
                var valueArray = new JS.ValueArrayPtr(count, (IntPtr)pContents);
                return JSAPI.NewArrayObject(context, ref valueArray);
            }
        }

        public uint Length {
            get {
                uint length;
                if (!JSAPI.GetArrayLength(Context, Root, out length))
                    throw new Exception("Operation failed");

                return length;
            }
            set {
                if (!JSAPI.SetArrayLength(Context, Root, value))
                    throw new Exception("Operation failed");
            }
        }

        /// <summary>
        /// Value becomes (or is) rooted by the object.
        /// It's your responsibility to root it if you need it to outlive the object.
        /// </summary>
        public JS.Value this[uint index] {
            get {
                var temp = new Rooted<JS.Value>(Context);
                if (!JSAPI.GetElement(Context, Root, index, temp))
                    throw new Exception("Operation failed");

                return temp.Value;
            }
            set {
                var temp = new Rooted<JS.Value>(Context, value);
                if (!JSAPI.SetElement(Context, Root, index, temp))
                    throw new Exception("Operation failed");
            }
        }
    }
}
