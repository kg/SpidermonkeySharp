SpidermonkeySharp
=================

Interact with the SpiderMonkey JavaScript runtime directly from C#. No custom native/managed bridge or wrapper DLL required. Just grab a recent build of mozjs.dll - from Firefox, for example - and get to work.

Provides managed representations of JSAPI data structures and exposes native JSAPI entry points. Sitting atop the native data structures & entry points are convenient wrapper functions that handle marshalling strings and other data types.

A second, higher-level layer on top of the wrapper functions provides managed classes and functions that dramatically simplify interacting with the VM, ideal for simple use cases. The high-level layer still exposes all the low-level pointers and values, so you can drop down to raw JSAPI when necessary.

This library is still early in development, so many feature additions & performance improvements remain to complete. Please feel free to report issues using the GitHub tracker at https://github.com/kg/SpidermonkeySharp/issues.

Features
========
Read and write object properties
Invoke JS functions
Expose native & managed functions to JS
Automatic mapping of managed delegates to SpiderMonkey JSNative functions
Rooting API for precise & correct interaction with JS garbage collector
