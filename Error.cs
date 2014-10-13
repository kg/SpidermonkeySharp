using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spidermonkey.Managed {
    public class JSError : JSObjectReference {
        public JSError (JSContextPtr context, JSObjectPtr obj)
            : base(context, obj) {
        }

        public JSObjectReference Constructor {
            get {
                return new JSObjectReference(
                    Context, base["constructor"]
                );
            }
        }

        public JSString ConstructorName {
            get {
                var constructor = Constructor;

                return JSString.New(
                    Context, constructor["name"]
                );
            }
        }

        public JSString Message {
            get {
                return JSString.New(
                    Context, base["message"]
                );
            }
        }

        public JSString FileName {
            get {
                return JSString.New(
                    Context, base["fileName"]
                );
            }
        }

        public int? LineNumber {
            get {
                var val = base["lineNumber"];
                if (val.IsNullOrUndefined)
                    return null;

                return (int)val;
            }
        }

        public JSString Stack {
            get {
                return JSString.New(
                    Context, base["stack"]
                );
            }
        }

        public override string ToString () {
            return String.Format(
                "{0}{1}{2}",
                base.ToString(),
                Environment.NewLine,
                Stack
            );
        }

        public JSRuntimeException ToException () {
            return new JSRuntimeException(this);
        }
    }

    public class JSRuntimeException : Exception {
        public JSRuntimeException (JSError error)
            : base(error.ToString()) {
        }
    }
}
