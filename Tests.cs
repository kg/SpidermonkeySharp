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
        [TestCase]
        public void BasicTest () {
            Assert.IsTrue(JSAPI.IsInitialized);

            var context = new JSContext(new JSRuntime());

            using (context.Request()) {
                var globalObject = new JSGlobalObject(context);

                using (context.EnterCompartment(globalObject)) {
                    Assert.IsTrue(JSAPI.InitStandardClasses(context, globalObject));

                    var evalResult = context.Evaluate(
                        globalObject, "var a = 'hello '; var b = 'world'; a + b"
                    );

                    var resultType = evalResult.Value.ValueType;
                    Assert.AreEqual(JSValueType.STRING, resultType);

                    Assert.AreEqual("hello world", evalResult.Value.ToManagedString(context));
                }
            }
        }
    }
}
