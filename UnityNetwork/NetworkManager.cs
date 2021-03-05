using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace UnityNetwork
{
    public class NetworkManager
    {
        // NetworkManager實作
        public System.Collections.ArrayList _socketList { get; private set; }
        private List<NetPacket> Packets;

        private List<string> keys;
        private Dictionary<string, NetPacket> PacketKey;

        public Dictionary<string, object> ToPeerTCPIP { get; private set; }
        public Dictionary<System.Net.Sockets.TcpClient, object> ToPeerTCP { get; private set; }
        public Dictionary<string, object> ToPeerUDPIP { get; private set; }
        public Dictionary<System.Net.IPEndPoint, object> ToPeerUDP { get; private set; }

        public NetworkManager()
        {
            Packets = new List<NetPacket>();
            PacketKey = new Dictionary<string, NetPacket>();
            _socketList = new System.Collections.ArrayList();
            ToPeerTCPIP = new Dictionary<string, object>();
            ToPeerTCP = new Dictionary<TcpClient, object>();
            ToPeerUDPIP = new Dictionary<string, object>();
            ToPeerUDP = new Dictionary<System.Net.IPEndPoint, object>();
            keys = new List<string>();
        }

        ~NetworkManager()
        {
            _socketList.Clear();
            Packets.Clear();
            PacketKey.Clear();
            keys.Clear();
            ToPeerTCPIP.Clear();
            ToPeerTCP.Clear();
            ToPeerUDPIP.Clear();
            ToPeerUDP.Clear();
            _socketList = null;
            Packets = null;
            PacketKey = null;
            keys = null;
            ToPeerTCPIP = null;
            ToPeerTCP = null;
            ToPeerUDPIP = null;
            ToPeerUDP = null;
        }

        public void Clear()
        {
            _socketList.Clear();
            Packets.Clear();
            PacketKey.Clear();
            keys.Clear();
            ToPeerTCPIP.Clear();
            ToPeerTCP.Clear();
            ToPeerUDPIP.Clear();
            ToPeerUDP.Clear();
        }

        // 資料包佇列
        public int PacketSize
        {
            get { return Packets.Count; }
        }

        public int PacketCount { get; set; } = 0;

        public string AddPacketKey()
        {
            string a;
            lock (keys)
            {
                for (a = Guid.NewGuid().ToString(); keys.Contains(a); a = Guid.NewGuid().ToString()) { }
                keys.Add(a);
            }
            return a;
        }

        // 數據包入隊
        public void AddPacket(string key, NetPacket packet)
        {
            ushort msgid = 0;
            packet.TOID(out msgid);
            if (msgid == (ushort)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT || msgid == (ushort)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT2)
            {
                lock (keys)
                {
                    lock (Packets)
                    {
                        packet.ChangeIDOnly(msgid == (ushort)MessageIdentifiers.ID.NOT_IMPORT_ID_CHAT ? (ushort)MessageIdentifiers.ID.ID_CHAT : (ushort)MessageIdentifiers.ID.ID_CHAT2);
                        Packets.Add(packet);
                        PacketCount++;
                        keys.Remove(key);
                    }
                }
            }
            else
            {
                lock (keys)
                {
                    PacketKey.Add(key, packet);
                    while (keys.Count != 0)
                    {
                        if (PacketKey.ContainsKey(keys[0]))
                        {
                            lock (Packets)
                            {
                                Packets.Add(PacketKey[keys[0]]);
                                PacketCount++;
                                PacketKey.Remove(keys[0]);
                                keys.RemoveAt(0);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        // 數據包出隊
        public NetPacket GetPacket()
        {
            lock (Packets)
            {
                if (Packets.Count == 0)
                    return null;

                NetPacket a = (NetPacket)Packets[0];
                Packets.RemoveAt(0);
                return a;
            }
        }

        public void CleanPacket()
        {
            Packets.Clear();
        }
    }
}

