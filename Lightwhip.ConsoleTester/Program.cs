
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Lightwhip.ConsoleTester;

const int ART_NET_PORT = 6454;

const int seconds = 1;
const int fps = 10;
const int totalFps = seconds * fps;
const double fpm = 1000.0 / (double)fps;

using var colorStream = new MemoryStream();
await PrepareTestData(colorStream, totalFps);

using var udpClient = new UdpClient();
//udpClient.AllowNatTraversal(true);
udpClient.Client.EnableBroadcast = true;
//udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, 1);

var wirelessBroadcast = GetWirelessBroadcast();
if(wirelessBroadcast is null)
{
    Console.WriteLine("Could not resolve wireless broadcast");
    return;
}
var wideBroadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, ART_NET_PORT);
udpClient.Connect(new IPEndPoint(wirelessBroadcast, ART_NET_PORT));

//var endpoint = new IPEndPoint(IPAddress.Parse("192.168.221.245"), ART_NET_PORT);
//var endpoint = new IPEndPoint(IPAddress.Parse("192.168.221.255"), ART_NET_PORT);
//udpClient.Connect(endpoint);

//udpClient.Connect(IPAddress.Broadcast, ART_NET_PORT);
//udpClient.Connect(IPAddress.Parse("192.168.221.255"), ART_NET_PORT);
//udpClient.Connect(IPAddress.Parse("192.168.50.13"), ART_NET_PORT);
//udpClient.Connect(IPAddress.Parse("192.168.221.245"), ART_NET_PORT);

using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(fpm));
var buffer = new byte[3];

while (colorStream.Position < colorStream.Length && await timer.WaitForNextTickAsync())
{
    await colorStream.ReadExactlyAsync(buffer, 0, 3);
    var packet = new ArtNetDmxPacket(buffer);

    await udpClient.SendAsync(packet.Data);

    Console.WriteLine($"{DateTime.Now:mm:ss:ffffff} - r({buffer[0]:D3}) g({buffer[1]:D3}) b({buffer[2]:D3})");
}


udpClient.Close();

Console.WriteLine();
Console.WriteLine("Done with sending ;)");

static async Task PrepareTestData(MemoryStream colorStream, int totalFps)
{
    var buffer = new byte[3];
    for (var i = 0; i < totalFps; i++)
    {
        buffer[0] = (byte)((i) % 255);
        buffer[1] = (byte)((i + 128) % 255);
        buffer[2] = (byte)((i + 75) % 255);

        colorStream.Write(buffer);
    }

    await colorStream.FlushAsync();
    colorStream.Position = 0;
}

static IPAddress? GetWirelessBroadcast()
{
    var wirelessInterface = NetworkInterface
        .GetAllNetworkInterfaces()
        .SingleOrDefault(x => x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && x.OperationalStatus == OperationalStatus.Up);
    if (wirelessInterface is null)
    {
        Console.WriteLine("Found no wireless interface");
        return null;
    }
    Console.WriteLine($"WLAN-Interface: {wirelessInterface.Name}");
    var wirelessLocalAddress = wirelessInterface
        .GetIPProperties()
        .UnicastAddresses
        .FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork);
    if (wirelessLocalAddress is null)
    {
        Console.WriteLine("Could not resolve network address");
        return null;
    }
    Console.WriteLine($"Wireless Local-IP: {wirelessLocalAddress.Address}");
    var wirelessBroadcast = NetworkHelper.GetBroadcastAddress(wirelessLocalAddress);
    Console.WriteLine($"Wireless Broadcast-IP: {wirelessBroadcast}");

    return wirelessBroadcast;
}

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
