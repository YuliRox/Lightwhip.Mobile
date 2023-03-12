
using System.Net;
using System.Net.Sockets;
using System.Text;

const int ART_NET_PORT = 6454;


var colorStream = new MemoryStream();

var buffer = new byte[3];
for (var i = 0; i < 100; i++)
{
    buffer[0] = (byte)((i) % 255);
    buffer[1] = (byte)((i + 128) % 255);
    buffer[2] = (byte)((i + 75) % 255);

    colorStream.Write(buffer);
}

await colorStream.FlushAsync();
colorStream.Position = 0;

using var udpClient = new UdpClient();
udpClient.Connect(IPAddress.Broadcast, ART_NET_PORT);

while (colorStream.Position < colorStream.Length)
{
    await colorStream.ReadExactlyAsync(buffer, 0, 3);
    var packet = new ArtnetPacket(buffer);

    await udpClient.SendAsync(packet.Data);

    Console.Write('.');
}

udpClient.Close();

Console.WriteLine();
Console.WriteLine("Done with sending ;)");

readonly struct ArtnetPacket
{
    private static readonly byte[] ART_ID = Encoding.ASCII.GetBytes("Art-Net");
    private const UInt16 ART_VERSION = 14;
    private const UInt16 ART_OPCODE = 0x5000;
    private static int SEQUENCE = 0;
    private static readonly byte[] ReferenceHeader = new byte[18];

    private readonly byte[] _bytes = new byte[20];

    static ArtnetPacket()
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


    public ArtnetPacket(byte[] data)
    {
        Buffer.BlockCopy(ReferenceHeader, 0, _bytes, 0, 18);

        _bytes[12] = (byte)Interlocked.Increment(ref SEQUENCE);
        if (_bytes[12] == 0)
            _bytes[12] = (byte)Interlocked.Increment(ref SEQUENCE);

        Buffer.BlockCopy(data, 0, _bytes, 17, 3);
    }

    public ArtnetPacket(byte red, byte green, byte blue)
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
