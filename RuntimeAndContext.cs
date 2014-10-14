using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Spidermonkey.Managed {
    public class JSRuntime : IDisposable {
        public readonly JSRuntimePtr Pointer;
        public bool IsDisposed { get; private set; }

        public JSRuntime (uint maxBytes = 1024 * 1024 * 8) {
            Pointer = JSAPI.NewRuntime(maxBytes);
            if (Pointer.IsZero)
                throw new Exception();
        }

        public static implicit operator JSRuntimePtr (JSRuntime obj) {
            return obj.Pointer;
        }

        public void Dispose () {
            if (IsDisposed)
                return;

            IsDisposed = true;
            GC.SuppressFinalize(this);

            JSAPI.DestroyRuntime(Pointer);
        }

        ~JSRuntime () {
            Dispose();
        }
    }

    public class JSContext : IDisposable {
        private static readonly ConcurrentDictionary<JSContextPtr, WeakReference>
            ContextRegistry = new ConcurrentDictionary<JSContextPtr, WeakReference>(
                JSContextPtr.Comparer
            );

        public readonly JSRuntimePtr Runtime;
        public readonly JSContextPtr Pointer;
        public readonly JSContextExceptionStatus Exception;
        public bool IsDisposed { get; private set; }

        public readonly WeakReference WeakSelf;

        public JSContext (JSRuntimePtr runtime) {
            Runtime = runtime;

            Pointer = JSAPI.NewContext(runtime, 8192);
            if (Pointer.IsZero)
                throw new Exception();

            Exception = new JSContextExceptionStatus(Pointer);
            WeakSelf = new WeakReference(this, true);

            if (!ContextRegistry.TryAdd(Pointer, WeakSelf))
                throw new Exception("Failed to update context registry");
        }

        public static JSContext FromPointer (JSContextPtr pointer) {
            WeakReference wr;

            if (!ContextRegistry.TryGetValue(pointer, out wr))
                return null;

            if (!wr.IsAlive)
                return null;

            JSContext result = (JSContext)wr.Target;
            return result;
        }

        public JSRequest Request () {
            return new JSRequest(this);
        }

        public JSCompartmentEntry EnterCompartment (JSObjectPtr obj) {
            return new JSCompartmentEntry(this, obj);
        }

        public static implicit operator JSContextPtr (JSContext obj) {
            return obj.Pointer;
        }

        public unsafe bool ReportUncaughtExceptions {
            get {
                var r = JSAPI.ContextOptionsRef(this);
                return !r->Options.HasFlag(JSContextOptionFlags.DontReportUncaught);
            }
            set {
                var r = JSAPI.ContextOptionsRef(this);
                var masked = 
                    (r->Options & ~JSContextOptionFlags.DontReportUncaught);

                if (value)
                    r->Options = masked;
                else
                    r->Options = masked | JSContextOptionFlags.DontReportUncaught;
            }
        }

        public Rooted<JS.Value> Evaluate (
            JSHandleObject scope, string scriptSource, out JSError error,
            string filename = null, uint lineNumber = 0
        ) {
            var prior = ReportUncaughtExceptions;
            try {
                ReportUncaughtExceptions = false;

                var isAlreadyThrowing = Exception.IsPending;

                var result = Evaluate(scope, scriptSource, filename, lineNumber);

                if (Exception.IsPending && !isAlreadyThrowing) {
                    var exc = Exception.Get();
                    if (exc.Value.ValueType == JSValueType.OBJECT) {
                        error = new JSError(this, exc.Value.AsObject);
                    } else {
                        exc = Exception.Get();
                        error = null;
                    }
                    Exception.Clear();
                } else {
                    error = null;
                }

                return result;
            } finally {
                ReportUncaughtExceptions = prior;
            }
        }

        public Rooted<JS.Value> Evaluate (JSHandleObject scope, string scriptSource, string filename = null, uint lineNumber = 0) {
            var resultRoot = new Rooted<JS.Value>(this, JS.Value.Undefined);

            if (JSAPI.EvaluateScript(
                this, scope,
                scriptSource, filename, lineNumber,
                resultRoot
            ))
                return resultRoot;

            resultRoot.Dispose();
            return null;
        }

        public void Dispose (bool collectGarbage = true) {
            if (IsDisposed)
                return;

            IsDisposed = true;
            GC.SuppressFinalize(this);

            WeakReference temp;
            if (!ContextRegistry.TryRemove(Pointer, out temp))
                Debug.WriteLine("Failed to remove context from registry");

            if (collectGarbage)
                JSAPI.DestroyContext(Pointer);
            else
                JSAPI.DestroyContextNoGC(Pointer);
        }

        void IDisposable.Dispose () {
            Dispose(true);
        }

        ~JSContext () {
            // Suppress GC since we're in a finalizer.
            Dispose(false);
        }
    }

    public struct JSRequest : IDisposable {
        private bool IsDisposed;

        public readonly JSContextPtr Context;

        public JSRequest (JSContextPtr context) {
            Context = context;
            IsDisposed = false;
            JSAPI.BeginRequest(Context);
        }

        public void Dispose () {
            if (IsDisposed)
                return;

            IsDisposed = true;
            JSAPI.EndRequest(Context);
        }
    }

    public struct JSCompartmentEntry : IDisposable {
        private bool IsDisposed;

        public readonly JSCompartmentPtr OldCompartment;
        public readonly JSContextPtr Context;

        public JSCompartmentEntry (JSContextPtr context, JSObjectPtr obj) {
            Context = context;
            IsDisposed = false;
            OldCompartment = JSAPI.EnterCompartment(context, obj);
        }

        public void Dispose () {
            if (IsDisposed)
                return;

            IsDisposed = true;
            JSAPI.LeaveCompartment(Context, OldCompartment);
        }
    }
}
