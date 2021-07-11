using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JimmikerNetwork
{
    public enum PacketType
    {
        #region Global Type
        ON_CONNECT,
        CONNECT_SUCCESSFUL,
        CONNECTION_LOST,
        CHECK,
        #endregion

        #region Server Local Type
        #endregion

        #region Client Local Type
        CONNECTION_ATTEMPT_FAILED,
        #endregion

        #region on Connect Remote Type
        RSAKEY,
        AESKEY,
        #endregion

        #region Send allow type
        /// <summary>
        /// Send allow type Top marker (Do not use in Packet)
        /// </summary>
        SendAllowTypeTop,

        ServerTell,
        Request,
        Response,

        /// <summary>
        /// Send allow type End marker (Do not use in Packet)
        /// </summary>
        SendAllowTypeEnd,
        #endregion

        #region P2P type
        /// <summary>
        /// P2P type Top marker (Do not use in Packet)
        /// </summary>
        P2PTypeTop,

        P2P_SERVER_CALL,
        P2P_SERVER_FAILED,
        P2P_GET_PUBLIC_ENDPOINT,
        P2P_CHECKING,
        P2P_CONNECTION,
        P2P_CONNECT_SUCCESSFUL,
        P2P_CONNECTION_LOST,

        #region P2P Send allow type
        /// <summary>
        /// P2P Send allow type Top marker (Do not use in Packet)
        /// </summary>
        P2PSendAllowTypeTop,

        P2P_Tell,

        /// <summary>
        /// P2P Send allow type End marker (Do not use in Packet)
        /// </summary>
        P2PSendAllowTypeEnd,
        #endregion


        /// <summary>
        /// P2P type End marker (Do not use in Packet)
        /// </summary>
        P2PTypeEnd
        #endregion
    }
}
