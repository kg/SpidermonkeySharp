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

        BACKGROUND_FINALIZE     = (1<<(16+8)),

        // HACK: !$#%(@!%)J)@!5()j2152j15
        GLOBAL_FLAGS = 0x2c000U
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

    public enum JSVersion : int {
        JSVERSION_ECMA_3 = 148,
        JSVERSION_1_6 = 160,
        JSVERSION_1_7 = 170,
        JSVERSION_1_8 = 180,
        JSVERSION_ECMA_5 = 185,
        JSVERSION_DEFAULT = 0,
        JSVERSION_UNKNOWN = -1,
        JSVERSION_LATEST = JSVERSION_ECMA_5
    };

    enum JSOverrideMode {
        Default,
        ForceTrue,
        ForceFalse
    };

    public enum JSValueType : byte {
        DOUBLE = 0x00,
        INT32 = 0x01,
        UNDEFINED = 0x02,
        BOOLEAN = 0x03,
        MAGIC = 0x04,
        STRING = 0x05,
        SYMBOL = 0x06,
        NULL = 0x07,
        OBJECT = 0x08,

        /* These never appear in a jsval; they are only provided as an out-of-band value. */
        UNKNOWN = 0x20,
        MISSING = 0x21
    }

    public enum JSValueTag : uint {
        CLEAR                = 0xFFFFFF80,
        INT32                = CLEAR | JSValueType.INT32,
        UNDEFINED            = CLEAR | JSValueType.UNDEFINED,
        STRING               = CLEAR | JSValueType.STRING,
        SYMBOL               = CLEAR | JSValueType.SYMBOL,
        BOOLEAN              = CLEAR | JSValueType.BOOLEAN,
        MAGIC                = CLEAR | JSValueType.MAGIC,
        NULL                 = CLEAR | JSValueType.NULL,
        OBJECT               = CLEAR | JSValueType.OBJECT    
    }

    [Flags]
    public enum JSContextOptionFlags : byte {
        PrivateNSISupports = 0x01,
        DontReportUncaught = 0x02
    }
}
