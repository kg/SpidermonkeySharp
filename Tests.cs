using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using Spidermonkey;

namespace Test {
    [TestFixture]
    public class Tests {
        void DefaultInit (out JSContext context, out JSGlobalObject globalObject) {
            Assert.IsTrue(JSAPI.IsInitialized);

            context = new JSContext(new JSRuntime());

            using (context.Request()) {
                globalObject = new JSGlobalObject(context);

                using (context.EnterCompartment(globalObject)) {
                    Assert.IsTrue(JSAPI.InitStandardClasses(context, globalObject));
                }
            }
        }

        [TestCase]
        public void BasicTest () {
            JSContext context;
            JSGlobalObject globalObject;
            DefaultInit(out context, out globalObject);

            using (context.Request())
            using (context.EnterCompartment(globalObject)) {
                var evalResult = context.Evaluate(
                    globalObject, "var a = 'hello '; var b = 'world'; a + b"
                );

                var resultType = evalResult.Value.ValueType;
                Assert.AreEqual(JSValueType.STRING, resultType);

                Assert.AreEqual("hello world", evalResult.Value.ToManagedString(context));
            }
        }

        [TestCase]
        public unsafe void ExceptionTest () {
            JSContext context;
            JSGlobalObject globalObject;
            DefaultInit(out context, out globalObject);

            // Suppress reporting of uncaught exceptions from Evaluate
            var options = JSAPI.ContextOptionsRef(context);
            options->Options = JSContextOptionFlags.DontReportUncaught;

            using (context.Request())
            using (context.EnterCompartment(globalObject)) {
                var evalResult = context.Evaluate(
                    globalObject, "function fn() { throw new Error('test'); }; fn()"
                );
                Assert.AreEqual(JS.Value.Undefined, evalResult.Value);

                Assert.IsTrue(JSAPI.IsExceptionPending(context));
                var exc = new Rooted<JS.Value>(context);
                Assert.IsTrue(JSAPI.GetPendingException(context, exc));
                JSAPI.ClearPendingException(context);

                Assert.AreEqual(JSValueType.OBJECT, exc.Value.ValueType);
                Assert.AreEqual("Error: test", exc.Value.ToManagedString(context));
            }
        }
    }
}
