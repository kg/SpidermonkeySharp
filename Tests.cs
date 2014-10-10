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
                    globalObject, "function fn() { throw new Error('test'); }; fn()", "eval"
                );
                Assert.AreEqual(JS.Value.Undefined, evalResult.Value);

                Assert.IsTrue(context.Exception.IsPending);
                var exc = context.Exception.Get();
                context.Exception.Clear();

                Assert.AreEqual(JSValueType.OBJECT, exc.Value.ValueType);
                Assert.AreEqual("Error: test", exc.Value.ToManagedString(context));
            }
        }

        [TestCase]
        public void GetPropertyTest () {
            JSContext context;
            JSGlobalObject globalObject;
            DefaultInit(out context, out globalObject);

            using (context.Request())
            using (context.EnterCompartment(globalObject)) {
                var evalResult = context.Evaluate(
                    globalObject, "var o = { 'a': 1, 'b': 'hello', 'c': 3.5 }; o"
                );

                Assert.AreEqual(JSValueType.OBJECT, evalResult.Value.ValueType);
                var objRoot = new Rooted<JSObjectPtr>(context, evalResult.Value.AsObject);

                var a = objRoot.Value.GetProperty(context, "a");
                Assert.AreEqual(JSValueType.INT32, a.Value.ValueType);
                Assert.AreEqual(1, (int)a.Value);

                var b = objRoot.Value.GetProperty(context, "b");
                Assert.AreEqual(JSValueType.STRING, b.Value.ValueType);
                Assert.AreEqual("hello", b.Value.ToManagedValue(context));

                var c = objRoot.Value.GetProperty(context, "c");
                Assert.AreEqual(JSValueType.DOUBLE, c.Value.ValueType);
                Assert.AreEqual(3.5, (double)c.Value);

                var d = objRoot.Value.GetProperty(context, "d");
                Assert.IsTrue(d.Value.IsNullOrUndefined);
            }
        }

        [TestCase]
        public void SetPropertyTest () {
            JSContext context;
            JSGlobalObject globalObject;
            DefaultInit(out context, out globalObject);

            using (context.Request())
            using (context.EnterCompartment(globalObject)) {
                var a = new JS.Value(37);
                var b = JS.Value.Null;

                globalObject.Pointer.SetProperty(context, "a", a);
                globalObject.Pointer.SetProperty(context, "b", b);

                var evalResult = context.Evaluate(
                    globalObject, "'a=' + a + ', b=' + b"
                );

                Assert.AreEqual("a=37, b=null", evalResult.Value.ToManagedString(context));
            }
        }

        [TestCase]
        public void InvokeTest () {
            JSContext context;
            JSGlobalObject globalObject;
            DefaultInit(out context, out globalObject);

            using (context.Request())
            using (context.EnterCompartment(globalObject)) {
                context.Evaluate(
                    globalObject, "function fortyTwo () { return 42; }; function double (i) { return i * 2; };"
                );

                {
                    var fn = globalObject.Pointer.GetProperty(context, "fortyTwo");
                    Assert.AreEqual(JSType.JSTYPE_FUNCTION, fn.Value.GetJSType(context));

                    var result = fn.Value.InvokeFunction(context, globalObject);
                    Assert.AreEqual(42, result.Value.ToManagedValue(context));
                }

                {
                    var fn = globalObject.Pointer.GetProperty(context, "double");
                    Assert.AreEqual(JSType.JSTYPE_FUNCTION, fn.Value.GetJSType(context));

                    var result = fn.Value.InvokeFunction(context, globalObject, new JS.Value(16));
                    Assert.AreEqual(32, result.Value.ToManagedValue(context));
                }
            }
        }
    }
}
