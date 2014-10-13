using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using JS;
using NUnit.Framework;
using Spidermonkey;
using Spidermonkey.Managed;

namespace Test {
    public class TestContext : IDisposable {
        public readonly JSRuntime Runtime;
        public readonly JSContext Context;
        public readonly JSGlobalObject Global;
        private readonly JSRequest Request;
        private readonly JSCompartmentEntry CompartmentEntry;

        public TestContext () {
            Assert.IsTrue(JSAPI.IsInitialized);

            Runtime = new JSRuntime();
            Context = new JSContext(Runtime);
            Request = Context.Request();
            Global = new JSGlobalObject(Context);
            CompartmentEntry = Context.EnterCompartment(Global);

            if (!JSAPI.InitStandardClasses(Context, Global))
                throw new Exception("Failed to initialize standard classes");
        }

        public static implicit operator JSRuntimePtr (TestContext self) {
            return self.Runtime.Pointer;
        }

        public static implicit operator JSContextPtr (TestContext self) {
            return self.Context.Pointer;
        }

        public void Dispose () {
            Global.Dispose();
            CompartmentEntry.Dispose();
            Request.Dispose();
            Context.Dispose();
            Runtime.Dispose();
        }
    }
    
    [TestFixture]
    public class Tests {
        [TestCase]
        public void BasicTest () {
            using (var tc = new TestContext()) {
                var evalResult = tc.Context.Evaluate(
                    tc.Global, 
                    @"var a = 'hello '; 
                      var b = 'world'; 
                      a + b"
                );

                var resultType = evalResult.Value.ValueType;
                Assert.AreEqual(JSValueType.STRING, resultType);

                Assert.AreEqual("hello world", evalResult.Value.ToManagedString(tc));
            }
        }

        [TestCase]
        public void ExceptionTest () {
            using (var tc = new TestContext()) {
                tc.Context.ReportUncaughtExceptions = false;

                var evalResult = tc.Context.Evaluate(
                    tc.Global, 
                    @"function fn() { 
                        throw new Error('test'); 
                      }; 
                      fn()"
                );
                Assert.AreEqual(JS.Value.Undefined, evalResult.Value);

                Assert.IsTrue(tc.Context.Exception.IsPending);
                var exc = tc.Context.Exception.Get();
                tc.Context.Exception.Clear();

                Assert.AreEqual(JSValueType.OBJECT, exc.Value.ValueType);
                Assert.AreEqual("Error: test", exc.Value.ToManagedString(tc));
            }
        }

        [TestCase]
        public void WrappedErrorTest () {
            using (var tc = new TestContext()) {
                tc.Context.ReportUncaughtExceptions = false;

                JSError evalError;
                tc.Context.Evaluate(
                    tc.Global,
                    @"function fn() { 
                        throw new Error('test'); 
                      }; 
                      fn()",
                    out evalError,
                    filename: "testScript"
                );

                Assert.IsNotNull(evalError);
                Assert.AreEqual(tc.Global["Error"].AsObject, evalError.Constructor.Pointer);
                Assert.AreEqual("Error", evalError.ConstructorName.ToString());
                Assert.AreEqual("test", evalError.Message.ToString());
                Assert.AreEqual("testScript", evalError.FileName.ToString());
                Assert.AreEqual(1, evalError.LineNumber);
            }
        }

        [TestCase]
        public void GetPropertyTest () {
            using (var tc = new TestContext()) {
                var evalResult = tc.Context.Evaluate(
                    tc.Global, 
                    @"var o = { 
                        'a': 1, 
                        'b': 'hello', 
                        'c': 3.5 
                      };
                      o"
                );

                Assert.AreEqual(JSValueType.OBJECT, evalResult.Value.ValueType);
                var obj = new JSObjectReference(evalResult);

                var a = obj["a"];
                Assert.AreEqual(JSValueType.INT32, a.ValueType);
                Assert.AreEqual(1, (int)a);

                var b = obj["b"];
                Assert.AreEqual(JSValueType.STRING, b.ValueType);
                Assert.AreEqual("hello", b.ToManagedValue(tc));

                var c = obj["c"];
                Assert.AreEqual(JSValueType.DOUBLE, c.ValueType);
                Assert.AreEqual(3.5, (double)c);

                var d = obj["d"];
                Assert.IsTrue(d.IsNullOrUndefined);
            }
        }

        [TestCase]
        public void SetPropertyTest () {
            using (var tc = new TestContext()) {
                tc.Global["a"] = new JS.Value(37);
                tc.Global["b"] = JS.Value.Null;

                var evalResult = tc.Context.Evaluate(
                    tc.Global, "'a=' + a + ', b=' + b"
                );

                Assert.AreEqual("a=37, b=null", evalResult.Value.ToManagedString(tc));
            }
        }

        [TestCase]
        public void InvokeTest () {
            using (var tc = new TestContext()) {
                tc.Context.Evaluate(
                    tc.Global, 
                    @"function fortyTwo () { return 42; }; 
                      function double (i) { return i * 2; };"
                );

                {
                    var fn = tc.Global.Pointer.GetProperty(tc, "fortyTwo");
                    Assert.AreEqual(JSType.JSTYPE_FUNCTION, fn.Value.GetJSType(tc));

                    var result = fn.Value.InvokeFunction(tc, tc.Global);
                    Assert.AreEqual(42, result.Value.ToManagedValue(tc));
                }

                {
                    var fn = tc.Global.Pointer.GetProperty(tc, "double");
                    Assert.AreEqual(JSType.JSTYPE_FUNCTION, fn.Value.GetJSType(tc));

                    var result = fn.Value.InvokeFunction(tc, tc.Global, new JS.Value(16));
                    Assert.AreEqual(32, result.Value.ToManagedValue(tc));
                }
            }
        }

        public static bool TestNative (JSContextPtr cx, uint argc, JSCallArgumentsPtr vp) {
            if (argc < 1)
                return false;

            var n = vp[0];
            vp.Result = new JS.Value((int)n * 2);
            return true;
        }

        [TestCase]
        public void DefineFunctionTest () {
            using (var tc = new TestContext()) {
                var native = (JSNative)TestNative;
                tc.Global.Pointer.DefineFunction(
                    tc, "test", native, 1
                );

                var evalResult = tc.Context.Evaluate(
                    tc.Global,
                    @"test(16)"
                );
                Assert.AreEqual(32, evalResult.Value.ToManagedValue(tc));
            }
        }

        public static int TestManaged (int i) {
            return i * 2;
        }

        [TestCase]
        public void DefineMarshalledFunctionTest () {
            using (var tc = new TestContext()) {
                var managed = (Func<int, int>)TestManaged;
                tc.Global.Pointer.DefineFunction(
                    tc, "test", managed
                );

                var evalResult = tc.Context.Evaluate(
                    tc.Global,
                    @"test(16)"
                );
                Assert.AreEqual(32, evalResult.Value.ToManagedValue(tc));
            }
        }

        [TestCase]
        public void ValueFromObject () {
            using (var tc = new TestContext()) {
                // Implicit conversion from Rooted<JSObjectPtr> to JS.Value
                JS.Value val = tc.Global.Root;
                tc.Global["g"] = val;

                var evalResult = tc.Context.Evaluate(tc.Global, "g");
                Assert.AreEqual(val, evalResult.Value);
                Assert.AreEqual(tc.Global.Pointer, evalResult.Value.AsObject);
            }
        }

        [TestCase]
        public void ObjectBuilderTest () {
            using (var tc = new TestContext())
            using (var obj = new JSObjectBuilder(tc)) {
                tc.Global["obj"] = obj;
                obj["a"] = new JS.Value(5);

                var evalResult = tc.Context.Evaluate(tc.Global, "obj.a");
                Assert.AreEqual(5, evalResult.Value.ToManagedValue(tc));
            }
        }

        [TestCase]
        public void ArrayReadTest () {
            using (var tc = new TestContext()) {
                var evalResult = tc.Context.Evaluate(tc.Global, "[1, 2, 3, 4]");

                Assert.IsTrue(JSAPI.IsArrayObject(tc, evalResult));

                var arrayHandle = (JSHandleObject)evalResult;

                uint length;
                Assert.IsTrue(JSAPI.GetArrayLength(tc, arrayHandle, out length));
                Assert.AreEqual(4, length);

                var elementRoot = new Rooted<JS.Value>(tc);

                Assert.IsTrue(JSAPI.GetElement(tc, arrayHandle, 0, elementRoot));
                Assert.AreEqual(1, elementRoot.Value.ToManagedValue(tc));

                Assert.IsTrue(JSAPI.GetElement(tc, arrayHandle, 3, elementRoot));
                Assert.AreEqual(4, elementRoot.Value.ToManagedValue(tc));
            }
        }

        [TestCase]
        public void ArrayWriteTest () {
            using (var tc = new TestContext()) {
                var array = new JSArray(tc, 3);

                array[0] = new Value(1.5);
                array[1] = new Value(5);
                array[2] = new JSString(tc, "hello");

                tc.Global["arr"] = array;

                var evalResult = tc.Context.Evaluate(tc.Global, "'[' + String(arr) + ']'");
                Assert.AreEqual("[1.5,5,hello]", evalResult.Value.ToManagedString(tc));
            }
        }

        [TestCase]
        public void ArrayFromManagedArray () {
            using (var tc = new TestContext()) {
                var array = new JSArray(tc, new[] {
                    new JS.Value(1),
                    new JS.Value(1.5),
                    new JS.Value(3),
                    new JSString(tc, "hello"),
                    JS.Value.Null,
                }, 1, 3);

                tc.Global["arr"] = array;

                var evalResult = tc.Context.Evaluate(tc.Global, "'[' + String(arr) + ']'");
                Assert.AreEqual("[1.5,3,hello]", evalResult.Value.ToManagedString(tc));

                Assert.AreEqual(new JS.Value(1.5), array[0]);
            }
        }

        [TestCase]
        public void StringTest () {
            using (var tc = new TestContext()) {
                var expected = "hello world";
                var s = new JSString(tc, expected);
                tc.Global["str"] = s;

                Assert.AreEqual(expected.Length, s.Length);

                var evalResult = tc.Context.Evaluate(tc.Global, "str + '!!!'");
                var evalResultS = new JSString(evalResult);
                Assert.AreEqual(expected + "!!!", evalResultS.ToString());
            }
        }

        public static bool TestNativeConstructor (JSContextPtr cx, uint argc, JSCallArgumentsPtr vp) {
            return true;
        }

        [TestCase]
        public void CustomClassTest () {
            using (var tc = new TestContext()) {
                var cc = new JSCustomClass(tc, "testClass", tc.Global);
                cc.Initialize();

                cc.Prototype["a"] = new JSString(tc, "hello");

                var jsCtor = tc.Global["testClass"];
                var classObj = new JSObjectReference(tc, jsCtor);

                Assert.AreEqual(cc.Prototype.Pointer, classObj["prototype"].AsObject);

                Assert.AreEqual("hello", tc.Context.Evaluate(tc.Global, "testClass.prototype.a").Value.ToManagedString(tc));

                var evalResult = tc.Context.Evaluate(tc.Global, "(new testClass())");
                var resultObj = new JSObjectReference(evalResult);

                Assert.AreEqual(jsCtor.AsObject, resultObj["constructor"].AsObject);

                Assert.AreEqual("hello", resultObj["a"].ToManagedString(tc));
            }
        }

        [TestCase]
        public void NewInstanceTest () {
            using (var tc = new TestContext()) {
                tc.Context.Evaluate(tc.Global, "function cls (x) { this.x = x; };");

                var cls = tc.Global["cls"];
                var arg1 = new JS.Value(1.5);
                var instance = new JSObjectReference(
                    tc, cls.InvokeConstructor(tc, arg1)
                );

                Assert.AreEqual(arg1, instance["x"]);
            }
        }

        [TestCase]
        public void GetPrototypeTest () {
            using (var tc = new TestContext()) {
                var evalResult = tc.Context.Evaluate(
                    tc.Global,
                    "[new String('a'), new Error('b'), []]"
                );

                var arr = new JSArray(evalResult);
                var s = new JSObjectReference(tc, arr[0]);
                var e = new JSObjectReference(tc, arr[1]);
                var a = new JSObjectReference(tc, arr[2]);

                var getClassProto = (Func<string, JSObjectReference>)( (name) => {
                    JSObjectPtr p;
                    if (!tc.Global.TryGetNested(out p, name, "prototype"))
                        throw new Exception("Prototype not found");

                    return new JSObjectReference(tc, p);
                });

                Assert.AreEqual(
                    getClassProto("String"), s.Prototype
                );
                Assert.AreEqual(
                    getClassProto("Error"), e.Prototype
                );
                Assert.AreEqual(
                    getClassProto("Array"), a.Prototype
                );
            }
        }
    }
}
