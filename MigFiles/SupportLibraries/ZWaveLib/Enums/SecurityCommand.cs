using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZWaveLib
{
    public enum SecurityCommand
    {
        COMMAND_SUPPORTED_GET = 0x02,
        COMMAND_SUPPORTED_REPORT = 0x03,
        COMMAND_SCHEME_GET = 0x04,
        COMMAND_SCHEME_REPORT = 0x05,
        COMMAND_NETWORK_KEY_SET = 0x06,
        COMMAND_NETWORK_KEY_VERIFY = 0x07,
        COMMAND_SCHEME_INHERIT = 0x08,
        COMMAND_NONCE_GET = 0x40,
        COMMAND_NONCE_REPORT = 0x80,
        COMMAND_MESSAGE_ENCAP = 0x81,
        COMMAND_MESSAGE_ENCAP_NONCE_GET = 0xc1
    }


    public enum SecurityScheme : byte
    {
        SECURITY_SCHEME_ZERO = 0x00,
        SECURITY_SCHEME_ONE = 0x01,
        SECURITY_SCHEME_RESERVED1 = 0x02,
        SECURITY_SCHEME_RESERVED2 = 0x04,
        SECURITY_SCHEME_RESERVED3 = 0x08,
        SECURITY_SCHEME_RESERVED4 = 0x10,
        SECURITY_SCHEME_RESERVED5 = 0x20,
        SECURITY_SCHEME_RESERVED6 = 0x40,
        SECURITY_SCHEME_RESERVED7 = 0x80
    }
}
