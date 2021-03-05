using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace UnityNetwork
{
    public class NetPacket
    {
        // 位元流
        public byte[] _bytes;

        // 相關的socket
        public TcpClient _peerTCP = null;
        public IPEndPoint _peerUDP = null;

        public Response response = null;

        // 包總長
        protected int _length = 0;

        // 錯誤資訊
        public string _error = "";

        // 初始化
        public NetPacket(int bytelength)
        {
            _bytes = new byte[bytelength];
        }

        // 從資料流程中拷貝資料
        public void CopyBytes(NetBitStream stream)
        {
            stream.BYTES.CopyTo(_bytes, 0);

            _length = stream.Length;

        }

        // 設置訊息識別字
        public void SetIDOnly(ushort msgid)
        {

            byte[] bs = System.BitConverter.GetBytes(msgid);

            bs.CopyTo(_bytes, NetBitStream.header_length);

            _length = NetBitStream.header_length + NetBitStream.SHORT16_LEN;

        }

        public void ChangeIDOnly(ushort msgid)
        {

            byte[] bs = System.BitConverter.GetBytes(msgid);

            bs.CopyTo(_bytes, NetBitStream.header_length);

        }

        // 取得訊息識別字
        public void TOID(out ushort msg_id)
        {
            msg_id = System.BitConverter.ToUInt16(_bytes, NetBitStream.header_length);
        }


    }
}

