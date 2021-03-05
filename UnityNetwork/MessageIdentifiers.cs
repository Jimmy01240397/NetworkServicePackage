using System.Collections.Generic;
using System.Text;

namespace UnityNetwork
{
    public class MessageIdentifiers
    {
        public enum ID
        {
            NULL = 0,

            //  服务器接受了客户端的连接请求
            CONNECTION_REQUEST_ACCEPTED,

            // 连接服务器失败
            CONNECTION_ATTEMPT_FAILED,

            // 失去连接
            CONNECTION_LOST,

            // 服务器接收到一个新的连接
            NEW_INCOMING_CONNECTION,

            LOADING_NOW,

            CHECKING,

            KEY,

            // 聊天专用ID 发送聊天回復消息
            ID_CHAT,

            // 聊天专用ID 发送聊天事件消息
            ID_CHAT2,

            // 聊天专用ID 发送非重要回復消息
            NOT_IMPORT_ID_CHAT,

            // 聊天专用ID 发送非重要事件消息
            NOT_IMPORT_ID_CHAT2,

            P2P_SERVER_CALL,

            P2P_CONNECTION,

            P2P_LOST,

            P2P_CHECKING,

            P2P_ID_CHAT,

            END
        };
    }
}
