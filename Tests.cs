using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using Spidermonkey;

namespace Test {
    public static class Program {
        public static void ErrorReporter (JSContextPtr context, string message, ref JSErrorReport report) {
            throw new Exception();
        }

        [STAThread]
        public unsafe static void Main () {
            Assert.IsTrue(JSAPI.Init());

            var runtime = JSAPI.NewRuntime(1024 * 1024 * 4);
            Assert.IsTrue(runtime.IsNonzero);

            var context = JSAPI.NewContext(runtime, 8192);
            Assert.IsTrue(context.IsNonzero);

            JSAPI.BeginRequest(context);

            JSErrorReporter errorReporter = ErrorReporter;

            // SetErrorReporter returns previously-active reporter
            Assert.AreEqual(null, JSAPI.SetErrorReporter(context, errorReporter));
            Assert.AreEqual(errorReporter, JSAPI.SetErrorReporter(context, errorReporter));

            var globalObject = JSAPI.NewGlobalObject(
                context, 
                ref JSClass.DefaultGlobalObjectClass,
                null,
                JSOnNewGlobalHookOption.DontFireOnNewGlobalHook,
                ref JSCompartmentOptions.Default
            );
            Assert.IsTrue(globalObject.IsNonzero);

            var oldCompartment = JSAPI.EnterCompartment(context, globalObject);
            Assert.IsTrue(oldCompartment.IsZero);

            var globalRoot = new Rooted<JSObjectPtr>(context, globalObject);
            Assert.IsTrue(JSAPI.InitStandardClasses(context, globalRoot));

            string testScript =
                @"'hello world'";
            string filename =
                @"test.js";

            IntPtr ptrTestScript = Marshal.StringToHGlobalUni(testScript);
            IntPtr ptrFilename = Marshal.StringToHGlobalAnsi(filename);

            var resultRoot = new Rooted<JS.Value>(context);

            var evalSuccess = JSAPI.EvaluateUCScript(
                context, globalRoot,
                ptrTestScript, testScript.Length,
                ptrFilename, 0,
                resultRoot
            );

            Assert.IsTrue(evalSuccess);

            var resultJsString = JSAPI.ToString(context, resultRoot);
            Assert.IsTrue(resultJsString.IsNonzero);
            var resultString = resultJsString.ToManagedString(context);

            Assert.AreEqual("hello world", resultString);

            JSAPI.LeaveCompartment(context, oldCompartment);
        }
    }
}
