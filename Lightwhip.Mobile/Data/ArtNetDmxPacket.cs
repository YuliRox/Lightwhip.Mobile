using System.Text;

namespace Lightwhip.Mobile.Data;

public readonly struct ArtNetDmxPacket
{
    private static readonly byte[] ART_ID = Encoding.ASCII.GetBytes("Art-Net");
    private const UInt16 ART_VERSION = 14;
    private const UInt16 ART_OPCODE = 0x5000;
    private static int SEQUENCE = 0;
    private static readonly byte[] ReferenceHeader = new byte[18];

    private readonly byte[] _bytes = new byte[21];

    static ArtNetDmxPacket()
    {
        Buffer.BlockCopy(ART_ID, 0, ReferenceHeader, 0, 7);

        ReferenceHeader[8] = 0;
        ReferenceHeader[9] = (byte)(ART_OPCODE >> 8);

        ReferenceHeader[10] = (byte)(ART_VERSION >> 8);
        ReferenceHeader[11] = (byte)(ART_VERSION);

        ReferenceHeader[13] = 0; //physical 0-3

        ReferenceHeader[14] = 0; //low universe
        ReferenceHeader[15] = 0; //>> 8 high universe

        const byte len = 4; // 3 + 1 to get an even number

        ReferenceHeader[16] = 0; //>> 8 high len
        ReferenceHeader[17] = len; //low len
    }


    public ArtNetDmxPacket(byte[] data)
    {
        Buffer.BlockCopy(ReferenceHeader, 0, _bytes, 0, 18);

        _bytes[12] = (byte)Interlocked.Increment(ref SEQUENCE);
        if (_bytes[12] == 0)
            _bytes[12] = (byte)Interlocked.Increment(ref SEQUENCE);

        Buffer.BlockCopy(data, 0, _bytes, 18, 3);
    }

    public ArtNetDmxPacket(byte red, byte green, byte blue)
    {
        Buffer.BlockCopy(ReferenceHeader, 0, _bytes, 0, 18);

        //TODO 0 is not allowed
        ReferenceHeader[12] = (byte)Interlocked.Increment(ref SEQUENCE);

        _bytes[18] = red;
        _bytes[19] = green;
        _bytes[20] = blue;
    }

    public ReadOnlyMemory<byte> Data { get => _bytes; }
}
