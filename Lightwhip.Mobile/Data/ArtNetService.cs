using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;

namespace Lightwhip.Mobile.Data;

public record class NetworkInterfaceDto
{
    public required string Id { get; init; }
    public required NetworkInterfaceType NetworkInterfaceType { get; init; }
    public required OperationalStatus OperationalStatus { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
}

public record class NetworkAddressDto
{

}

public class ArtNetService
{
    public IEnumerable<NetworkInterfaceDto> ListAvailableInterfaces()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(x => x.OperationalStatus == OperationalStatus.Up)
            .Select(x => new NetworkInterfaceDto()
            {
                Id = x.Id,
                NetworkInterfaceType = x.NetworkInterfaceType,
                OperationalStatus = x.OperationalStatus,
                Name = x.Name,
                Description = x.Description
            });

        /*var wirelessInterface = NetworkInterface
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
        var wirelessBroadcast = GetBroadcastAddress(wirelessLocalAddress);
        Console.WriteLine($"Wireless Broadcast-IP: {wirelessBroadcast}");

        return wirelessBroadcast;*/
    }

    public IEnumerable<NetworkAddressDto> ListAvailableAddresses(string networkId)
    {
        var network = NetworkInterface.GetAllNetworkInterfaces().Single(x => x.Id == networkId);
        throw new NotImplementedException();
    }

    public async Task BroadcastMessagesAsync(CancellationToken cancellationToken = default)
    {
        const int ART_NET_PORT = 6454;

        const int seconds = 1;
        const int fps = 10;
        const int totalFps = seconds * fps;
        const double fpm = 1000.0 / (double)fps;

        using var colorStream = new MemoryStream();
        await PrepareTestData(colorStream, totalFps);

        using var udpClient = new UdpClient();
        udpClient.Client.EnableBroadcast = true;
        //var wirelessBroadcast = GetWirelessBroadcast() ?? throw new InvalidOperationException();
        //udpClient.Connect(new IPEndPoint(wirelessBroadcast, ART_NET_PORT));

        //udpClient.Connect(IPAddress.Broadcast, ART_NET_PORT);
        var endpoint = new IPEndPoint(IPAddress.Parse("192.168.221.255"), ART_NET_PORT);
        udpClient.Connect(endpoint);

        //udpClient.Connect(IPAddress.Parse("192.168.221.245"), ART_NET_PORT);
        //udpClient.Connect(IPAddress.Parse("192.168.50.13"), ART_NET_PORT);

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(fpm));
        var buffer = new byte[3];

        while (colorStream.Position < colorStream.Length && await timer.WaitForNextTickAsync(cancellationToken))
        {
            await colorStream.ReadExactlyAsync(buffer, 0, 3, cancellationToken);
            var packet = new ArtNetDmxPacket(buffer);

            await udpClient.SendAsync(packet.Data, cancellationToken);
        }

        udpClient.Close();
    }

    private static async Task PrepareTestData(MemoryStream colorStream, int totalFps)
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

    private static IPAddress? GetWirelessBroadcast()
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
        var wirelessBroadcast = GetBroadcastAddress(wirelessLocalAddress);
        Console.WriteLine($"Wireless Broadcast-IP: {wirelessBroadcast}");

        return wirelessBroadcast;
    }

    public static IPAddress GetBroadcastAddress(IPAddress address, IPAddress mask)
    {
        uint ipAddress = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
        uint ipMaskV4 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
        uint broadCastIpAddress = ipAddress | ~ipMaskV4;

        return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
    }

    public static IPAddress GetBroadcastAddress(UnicastIPAddressInformation unicastAddress)
    {
        return GetBroadcastAddress(unicastAddress.Address, unicastAddress.IPv4Mask);
    }
}
