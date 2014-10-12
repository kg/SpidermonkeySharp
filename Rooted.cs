using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Spidermonkey.Managed;

namespace Spidermonkey {
    public interface IRootable {
        bool AddRoot (JSContextPtr context, JSRootPtr root);
        void RemoveRoot (JSContextPtr context, JSRootPtr root);
    }

    public class Rooted<T> : IDisposable 
        where T : struct, IRootable
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe class _State {
            public T Value;

            public _State (T value) {
                Value = value;
            }
        }

        private readonly WeakReference ManagedContextReference;
        public readonly JSContextPtr Context;

        public readonly GCHandle Pin;
        public readonly _State State;
        public readonly JSRootPtr Root;
        public bool IsDisposed { get; private set; }

        public bool IsFinalizerEnabled {
            get {
                return (ManagedContextReference != null);
            }
        }

        private Rooted (
            JSContext managedContext,
            JSContextPtr context, 
            T value
        ) {
            if (context.IsZero)
                throw new ArgumentNullException("context");

            // HACK: Attempt to locate a managed context for a raw pointer if that's all we have.
            // This lets us finalize safely.
            if (managedContext == null)
                managedContext = JSContext.FromPointer(context);

            if (managedContext != null)
                ManagedContextReference = managedContext.WeakSelf;
            else
                ManagedContextReference = null;

            Context = context;

            State = new _State(value);
            Pin = GCHandle.Alloc(State, GCHandleType.Pinned);
            Root = new JSRootPtr(Pin.AddrOfPinnedObject());

            if (!default(T).AddRoot(context, Root))
                throw new Exception("Failed to add root");
        }

        public Rooted (
            JSContext managedContext,
            T value = default(T)
        ) : this(managedContext, managedContext, value) {
        }

        /// <summary>
        /// WARNING: Roots created using this constructor will leak if not disposed.
        /// </summary>
        public Rooted (
            JSContextPtr context,
            T value = default(T)
        ) : this(null, context, value) {
        }

        public void Dispose () {
            if (IsDisposed)
                return;

            IsDisposed = true;
            GC.SuppressFinalize(this);

            default(T).RemoveRoot(Context, Root);
            Pin.Free();
        }

        ~Rooted () {
            if (IsDisposed)
                return;

            // Determine whether it is safe to delete this root.
            // If the owning context is destroyed, we will crash when unrooting.
            // On the other hand, a dead context doesn't have roots anymore. :-)
            bool canDispose = true;
            JSContext mc;

            if (ManagedContextReference == null)
                canDispose = false;
            else if (!ManagedContextReference.IsAlive)
                canDispose = false;
            else if ((mc = (JSContext)ManagedContextReference.Target) == null)
                canDispose = false;
            else if (mc.IsDisposed)
                canDispose = false;

            if (canDispose)
                Dispose();
        }

        public T Value {
            get {
                return State.Value;
            }
            set {
                State.Value = value;
            }
        }

        public static implicit operator T (Rooted<T> rooted) {
            return rooted.State.Value;
        }
    }
}
