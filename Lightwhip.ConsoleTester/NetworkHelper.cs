using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Lightwhip.ConsoleTester
{
    internal class NetworkHelper
    {
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
}
