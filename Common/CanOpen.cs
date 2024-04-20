using System;
using System.Runtime.InteropServices;

namespace USB_FTDI
{
    enum SDO_ComandByte
    {
        WriteReadSPI1Byte = (byte)1,
        WriteReadSPI2Byte = (byte)2,
        WriteReadSPI3Byte = (byte)3,
        WriteReadSPI4Byte = (byte)4,
        ReadSPIData = (byte)5,
        CastomCMDChangeMode = (byte)6,
        ADS1298_StateMachine = 7,
    };


    enum SDO_ParamIndex_e
    {
        sdoReadReg = (ushort)0,
        sdoWriteReg = (ushort)1,
        sdoTestMsg = (ushort)2,
        sdoStopMsg = (ushort)3,
        sdoWriteRead = (ushort)4,
        sdoReadContinunuos = (ushort)5,
    };

    enum SDO_Answer
    {
        sdoAns_DataContinuos = (byte)0x1,
        sdoAns_SuccessfulRead = (byte)0x4b,
        sdoAns_SuccessfulWrite = (byte)0x60,
        sdoAns_ErrorReadOrWrite = (byte)0x80,
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe public struct CanOpen_t
    {
        public byte cmd;
        public ushort index;
        public byte subindex;
        public uint data;
    }
}
