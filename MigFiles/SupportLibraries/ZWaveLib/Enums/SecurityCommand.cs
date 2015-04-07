using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZWaveLib
{
    public enum SecurityCommand
    {
        SupportedGet = 0x02,
        SupportedReport = 0x03,
        SchemeGet = 0x04,
        SchemeReport = 0x05,
        NetworkKeySet = 0x06,
        NetworkKeyVerify = 0x07,
        SchemeInherit = 0x08,
        NonceGet = 0x40,
        NonceReport = 0x80,
        MessageEncap = 0x81,
        MessageEncapNonceGet = 0xc1
    }


    public enum SecurityScheme : byte
    {
        SchemeZero = 0x00,
        SchemeOne = 0x01,
        SchemeReserved1 = 0x02,
        SchemeReserved2 = 0x04,
        SchemeReserved3 = 0x08,
        SchemeReserved4 = 0x10,
        SchemeReserved5 = 0x20,
        SchemeReserved6 = 0x40,
        SchemeReserved7 = 0x80
    }
}
