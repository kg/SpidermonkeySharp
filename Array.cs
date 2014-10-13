using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spidermonkey.Managed {
    public class JSArray : IDisposable {
        public readonly JSContextPtr Context;
        public readonly Rooted<JSObjectPtr> Root;

        private Rooted<JS.Value> LazyRootedValue;

        public JSArray(
            JSContextPtr context,
            JSObjectPtr obj
        ) {
            if (context.IsZero)
                throw new ArgumentNullException("context");

            Context = context;
            Root = new Rooted<JSObjectPtr>(Context, obj);

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

        public JSObjectPtr Pointer {
            get {
                return Root.Value;
            }
        }

        public void Dispose () {
            Root.Dispose();
        }

        // Since we're rooted it's safe to implicitly convert to a value
        public static implicit operator JS.Value (JSArray self) {
            return new JS.Value(self.Root);
        }

        // We have to lazily construct a rooted JS.Value for ourselves so that
        //  it can serve as the address of our JS.Value in order to produce a JSHandleValue.
        // Gross.
        public static implicit operator JSHandleValue (JSArray self) {
            if (self.LazyRootedValue == null)
                self.LazyRootedValue = new Rooted<JS.Value>(self.Context, self.Root);

            return self.LazyRootedValue;
        }

        public static implicit operator JSObjectPtr (JSArray self) {
            return self.Root.Value;
        }

        public static implicit operator JSHandleObject (JSArray self) {
            return self.Root;
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

        public override int GetHashCode () {
            return Pointer.GetHashCode();
        }

        // FIXME: Deep comparison?
        public bool Equals (JSArray rhs) {
            return Pointer.Equals(rhs.Pointer);
        }

        public override bool Equals (object obj) {
            var rhs = obj as JSArray;
            if (rhs != null)
                return Equals(rhs);
            else
                return base.Equals(obj);
        }

    }
}
