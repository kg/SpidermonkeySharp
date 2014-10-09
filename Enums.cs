using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spidermonkey {
    public enum JSOnNewGlobalHookOption : int {
        FireOnNewGlobalHook,
        DontFireOnNewGlobalHook
    };

    [Flags]
    public enum JSClassFlags : uint {
        HAS_PRIVATE             = (1<<0), // objects have private slot
        NEW_ENUMERATE           = (1<<1), // has JSNewEnumerateOp hook
        NEW_RESOLVE             = (1<<2), // has JSNewResolveOp hook
        PRIVATE_IS_NSISUPPORTS  = (1<<3), // private is (nsISupports *)
        IS_DOMJSCLASS           = (1<<4), // objects are DOM
        IMPLEMENTS_BARRIERS     = (1<<5), // Correctly implements GC read
                                                  // and write barriers
        EMULATES_UNDEFINED      = (1<<6), // objects of this class act
                                                  // like the value undefined,
                                                  // in some contexts
        USERBIT1                = (1<<7), // Reserved for embeddings.

        IS_ANONYMOUS            = (1<<(16+0)),
        IS_GLOBAL               = (1<<(16+1)),
        INTERNAL_FLAG2          = (1<<(16+2)),
        INTERNAL_FLAG3          = (1<<(16+3)),

        IS_PROXY                = (1<<(16+4)),

        // Bit 22 unused.

        // Reserved for embeddings.
        USERBIT2                = (1<<(16+6)),
        USERBIT3                = (1<<(16+7)),

        BACKGROUND_FINALIZE     = (1<<(16+8))
    }

    public enum JSType {
        JSTYPE_VOID,                /* undefined */
        JSTYPE_OBJECT,              /* object */
        JSTYPE_FUNCTION,            /* function */
        JSTYPE_STRING,              /* string */
        JSTYPE_NUMBER,              /* number */
        JSTYPE_BOOLEAN,             /* boolean */
        JSTYPE_NULL,                /* null */
        JSTYPE_SYMBOL,              /* symbol */
        JSTYPE_LIMIT
    };
}
