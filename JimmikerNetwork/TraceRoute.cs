using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace JimmikerNetwork
{
    public static class TraceRoute
    {
        private const string Data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        /// <summary>
        /// tracert命令
        /// </summary>
        /// <param name="hostNameOrAddress"></param>
        /// <returns></returns>
        public static List<IPAddress> GetTraceRoute(string hostNameOrAddress, int timeout)
        {
            return GetTraceRoute(hostNameOrAddress, timeout, 1);
        }

        /// <summary>
        /// 获取路由表
        /// </summary>
        /// <param name="hostNameOrAddress">地址</param>
        /// <param name="ttl">TTL参数</param>
        /// <returns></returns>
        private static List<IPAddress> GetTraceRoute(string hostNameOrAddress, int timeout, int ttl)
        {
            Dictionary<int, PingReply> result = new Dictionary<int, PingReply>();
            byte[] buffer = Encoding.ASCII.GetBytes(Data);

            Func<PingOptions, PingReply> pingsend = (pingerOptions) =>
            {
                Ping pinger = new Ping();
                PingReply reply = pinger.Send(hostNameOrAddress, timeout, buffer, pingerOptions);
                return reply;
            };

            void pingcallback(IAsyncResult ar)
            {
                PingOptions pingerOptions = (PingOptions)ar.AsyncState;
                lock (result)
                {
                    PingReply reply = pingsend.EndInvoke(ar);
                    result.Add(pingerOptions.Ttl, reply);
                }
            }

            for (int i = ttl, cont = 0; cont < 40; i++, cont++)
            {
                // 创建PingOptions对象
                PingOptions pingerOptions = new PingOptions(i, true);
                // 创建PingReply对象

                pingsend.BeginInvoke(pingerOptions, pingcallback, pingerOptions);
            }

            List<IPAddress> data = new List<IPAddress>();
            for (int i = 1, nullcont = 0; i <= 40 && nullcont < 5; i++)
            {
                SpinWait.SpinUntil(() => result.ContainsKey(i));
                if (result[i].Address == null)
                {
                    nullcont++;
                }
                else
                {
                    nullcont = 0;
                }
                data.Add(result[i].Address);
                if (result[i].Status == IPStatus.Success) break;
            }
            return data;
        }

        public static IPEndPoint IPEndPointParse(string endpointstring, AddressFamily MapTo)
        {
            string[] values = endpointstring.Split(new char[] { ':' });
            if (2 > values.Length)
            {
                throw new FormatException("Invalid endpoint format");
            }
            IPAddress ipaddress;
            string ipaddressstring = string.Join(":", values.Take(values.Length - 1).ToArray());
            if (!IPAddress.TryParse(ipaddressstring, out ipaddress))
            {
                throw new FormatException(string.Format("Invalid endpoint ipaddress '{0}'", ipaddressstring));
            }
            int port;
            if (!int.TryParse(values[values.Length - 1], out port)
             || port < IPEndPoint.MinPort
             || port > IPEndPoint.MaxPort)
            {
                throw new FormatException(string.Format("Invalid end point port '{0}'", values[values.Length - 1]));
            }

            switch(MapTo)
            {
                case AddressFamily.InterNetwork:
                    {
                        ipaddress = ipaddress.MapToIPv4();
                        break;
                    }
                case AddressFamily.InterNetworkV6:
                    {
                        ipaddress = ipaddress.MapToIPv6();
                        break;
                    }
            }

            return new IPEndPoint(ipaddress, port);
        }

        public static IPAddress[] RemoveIPV6(this IPAddress[] addresses)
        {
            List<IPAddress> iPAddresses = new List<IPAddress>(addresses);
            for (int i = 0; i < addresses.Length; i++)
            {
                if (addresses[i].AddressFamily == AddressFamily.InterNetworkV6)
                {
                    iPAddresses.Remove(addresses[i]);
                }
            }
            return iPAddresses.ToArray();
        }

        public static IPAddress MapToIPv6(this IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return address;
            }
            ushort[] data = new ushort[8]
            {
        0,
        0,
        0,
        0,
        0,
        65535,
        (ushort)(((address.Address & 0xFF00) >> 8) | ((address.Address & 0xFF) << 8)),
        (ushort)(((address.Address & 4278190080u) >> 24) | ((address.Address & 0xFF0000) >> 8))
            };
            byte[] getdata = new byte[16];

            int num = 0;
            for (int i = 0; i < 8; i++)
            {
                getdata[num++] = (byte)((data[i] >> 8) & 0xFF);
                getdata[num++] = (byte)(data[i] & 0xFF);
            }
            return new IPAddress(getdata, 0u);
        }

        public static IPAddress MapToIPv4(this IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                return address;
            }
            byte[] data = address.GetAddressBytes();
            ushort[] m_Numbers = new ushort[8];
            for (int i = 0; i < 8; i++)
            {
                m_Numbers[i] = (ushort)(data[i * 2] * 256 + data[i * 2 + 1]);
            }

            long newAddress = (uint)((int)((uint)(m_Numbers[6] & 0xFF00) >> 8) | ((m_Numbers[6] & 0xFF) << 8) | (((int)((uint)(m_Numbers[7] & 0xFF00) >> 8) | ((m_Numbers[7] & 0xFF) << 8)) << 16));
            return new IPAddress(newAddress);
        }

        public static bool IsIPv6Unicast(this IPAddress address)
        {
            return address.IsInSubnet("[2000::]/3");
        }

        public static bool IsInSubnet(this IPAddress address, string subnetMask)
        {
            if (address == null) return false;
            var slashIdx = subnetMask.IndexOf("/");
            if (slashIdx == -1)
            { // We only handle netmasks in format "IP/PrefixLength".
                throw new NotSupportedException("Only SubNetMasks with a given prefix length are supported.");
            }

            // First parse the address of the netmask before the prefix length.
            var maskAddress = IPAddress.Parse(subnetMask.Substring(0, slashIdx));

            if (maskAddress.AddressFamily != address.AddressFamily)
            { // We got something like an IPV4-Address for an IPv6-Mask. This is not valid.
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    address = address.MapToIPv6();
                }
                if (maskAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    maskAddress = maskAddress.MapToIPv6();
                }
            }

            // Now find out how long the prefix is.
            int maskLength = int.Parse(subnetMask.Substring(slashIdx + 1));

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                // Convert the mask address to an unsigned integer.
                var maskAddressBits = BitConverter.ToUInt32(maskAddress.GetAddressBytes().Reverse().ToArray(), 0);

                // And convert the IpAddress to an unsigned integer.
                var ipAddressBits = BitConverter.ToUInt32(address.GetAddressBytes().Reverse().ToArray(), 0);

                // Get the mask/network address as unsigned integer.
                uint mask = uint.MaxValue << (32 - maskLength);

                // https://stackoverflow.com/a/1499284/3085985
                // Bitwise AND mask and MaskAddress, this should be the same as mask and IpAddress
                // as the end of the mask is 0000 which leads to both addresses to end with 0000
                // and to start with the prefix.
                return (maskAddressBits & mask) == (ipAddressBits & mask);
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // Convert the mask address to a BitArray.
                var maskAddressBits = new BitArray(maskAddress.GetAddressBytes());

                // And convert the IpAddress to a BitArray.
                var ipAddressBits = new BitArray(address.GetAddressBytes());

                if (maskAddressBits.Length != ipAddressBits.Length)
                {
                    throw new ArgumentException("Length of IP Address and Subnet Mask do not match.");
                }

                // Compare the prefix bits.
                for(int i = 0; i < 128 && i < maskLength; i += 8)
                {
                    for(int j = 0; j < 8 && i * 8 + j < maskLength; j++)
                    {
                        if (ipAddressBits[i * 8 + 7 - j] != maskAddressBits[i * 8 + 7 - j])
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            throw new NotSupportedException("Only InterNetworkV6 or InterNetwork address families are supported.");
        }
    }
}