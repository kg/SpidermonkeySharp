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
        public static void ErrorReporter (JSContextPtr context, string message, ref JSErrorReport report) {
            throw new Exception();
        }

        [TestCase]
        public unsafe void BasicTest () {
            Assert.IsTrue(JSAPI.Init());

            var runtime = new JSRuntime();
            var context = new JSContext(runtime);

            using (context.Request()) {
                JSErrorReporter errorReporter = ErrorReporter;
                Assert.AreEqual(null, JSAPI.SetErrorReporter(context, errorReporter));

                var globalRoot = new Rooted<JSObjectPtr>(
                    context,
                    JSAPI.NewGlobalObject(
                        context,
                        ref JSClass.DefaultGlobalObjectClass,
                        null,
                        JSOnNewGlobalHookOption.DontFireOnNewGlobalHook,
                        ref JSCompartmentOptions.Default
                    )
                );
                Assert.IsTrue(globalRoot.Value.IsNonzero);

                using (context.EnterCompartment(globalRoot)) {
                    Assert.IsTrue(JSAPI.InitStandardClasses(context, globalRoot));
                    var evalResult = context.Evaluate(
                        globalRoot, "'hello world'"
                    );

                    var resultType = evalResult.Value.ValueType;
                    Assert.AreEqual(JSValueType.STRING, resultType);

                    Assert.AreEqual("hello world", evalResult.Value.ToManagedString(context));
                }
            }
        }
    }
}
