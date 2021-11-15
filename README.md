# NetworkServicePackage

A Network Service Package for C#. It have tcp, udp, p2p mode. You can use this to make your own game service, chat service, file service... 

* [Installation](https://github.com/Jimmy01240397/NetworkServicePackage#installation)
* [Overview](https://github.com/Jimmy01240397/NetworkServicePackage#overview)
* [Usage](https://github.com/Jimmy01240397/NetworkServicePackage#usage)
* [Example](https://github.com/Jimmy01240397/NetworkServicePackage#example)

## Installation

Import JimmikerNetwork.dll in your C# project and put JimmikerNetwork.xml that you can see a summary.

## Overview

In the project the following class are available to you:

### Global

| class/struct/interface |                 description                 |
| ------------------ | ----------------------------------------------- |
| INetClient         | a interface of your client controller           |
| INetServer         | a interface of your server controller           |
| Packet             | a unit for your network packet                  |
| SendData           | a data in your network packet                   |
| EncryptAndCompress  | a static class for Encrypt AndCompress |
| TraceRoute         | a static class for icmp TraceRouting and some network tool |

### Server

| class/struct/interface |                 description                 |
| ------------------ | ----------------------------------------------- |
| AppllicationBase   | a abstract class server management. Inherit it when you use it |
| PeerBase           | a abstract class unit for your network peer. Inherit it when you use it |

### Client

| class/struct/interface |                 description                 |
| ------------------ | ----------------------------------------------- |
| ClientListen   | a interface of your client management               |
| ClientLinker   | a client connect controller                         |
| PeerForP2PBase | a abstract class unit for your P2P peer. Inherit it when you use it |

## Usage
### Server
#### Using Namespace
``` C#
using JimmikerNetwork;
using JimmikerNetwork.Server;
```
#### Inheritance AppllicationBase
``` C#
public class Appllication : AppllicationBase
{
    public Appllication(IPAddress IP, int port, ProtocolType protocol):base(IP, port, protocol)
    {
        
    }

    protected override void Setup()
    {
        //Do something when server setup.
        RunUpdateThread(); //start Appllication.Update()'s thread.
    }

    protected override PeerBase AddPeerBase(object _peer, INetServer server)
    {
        //Do something when you get new connect.
        return new Peer(_peer, server, this);
    }

    protected override void TearDown()
    {
        //Do something when server stop.
    }

    protected override void DebugReturn(MessageType messageType, string msg)
    {
        //Do something when you get debug message.
    }
}
```

#### Inheritance PeerBase
``` C#
public class Peer : PeerBase
{
    public Peer(object peer, INetServer server) : base(peer, server)
    {
        
    }

    public override void OnOperationRequest(SendData sendData)
    {
        //Do something when get peer request.
        //like:
        switch(sendData.Code)
        {
            case 0:
            {
                Console.WriteLine(((object[])sendData.Parameters)[1].ToString());
            }
            case 1:
            {
                Console.WriteLine(sendData.Parameters.ToString());
            }
        }
    }

    public override void OnDisconnect()
    {
        //Do something when peer disconnect.
    }
}
```

#### Peer sent Response and Event
``` C#
peer.Reply(0, new object[]{ 100, "This is Response message 1" }, 0, "Debug Message");
peer.Reply(1, "This is Response message 2", 0, "Debug Message");
peer.Tell(0, new object[]{ 100, "This is Event message" });
```

#### Start Service
``` C#
Appllication appllication = new Appllication(<local ip>, <local port>, System.Net.Sockets.ProtocolType.<Tcp or Udp>);
appllication.Start();
```

#### Stop Service
``` C#
appllication.Disconnect();
appllication.StopUpdateThread();
```

### Client
#### Using Namespace
``` C#
using JimmikerNetwork;
using JimmikerNetwork.Client;
```

#### Inheritance ClientListen and Initialize ClientLinker
``` C#
public class Client : ClientListen
{
    public ClientLinker clientLinker { get; private set; }
    public Client(ProtocolType protocol)
    {
        clientLinker = new ClientLinker(this, protocol);
    }

    public void DebugReturn(string message)
    {
        //Do something when you get debug message.
    }
    public void OnEvent(SendData sendData)
    {
        //Do something when get server event.
        //like:
        switch(sendData.Code)
        {
            case 0:
            {
                Console.WriteLine(((object[])sendData.Parameters)[1].ToString());
            }
        }
    }
    public void OnOperationResponse(SendData sendData)
    {
        //Do something when get server response.
        //like:
        switch(sendData.Code)
        {
            case 0:
            {
                Console.WriteLine(((object[])sendData.Parameters)[1].ToString());
            }
            case 1:
            {
                Console.WriteLine(sendData.Parameters.ToString());
            }
        }
    }
    public void OnStatusChanged(LinkCobe connect)
    {
        //Do something when connect status change.
    }
    public PeerForP2PBase P2PAddPeer(object _peer, object publicIP, INetClient client, bool NAT)
    {
        //Do something when P2P peer connect.
        //If you don't enable P2P, you can just:
        throw new NotImplementedException();
    }
}
```

#### Write a connect function in Client class
``` C#
public class Client : ClientListen
{
    .
    .
    .
    public bool Connect(string host, int port)
    {
        bool on = clientLinker.Connect(host, port);
        clientLinker.RunUpdateThread();
        return on;
    }
    .
    .
    .
}
```

#### Client sent Request
``` C#
client.clientLinker.Ask(0, new object[]{ 100, "This is Request message 1" });
client.clientLinker.Ask(1, "This is Response message 2");
```

#### Start Client
``` C#
Client client = new Client(System.Net.Sockets.ProtocolType.<Tcp or Udp>);
client.Connect(<remote host>, <remote port>);
```

#### Stop Client
``` C#
client.clientLinker.Disconnect();
client.clientLinker.StopUpdateThread();
```

### P2P
#### Client Allow P2P mode
``` C#
client.clientLinker.EnableP2P = true;
//or                                                         EnableP2P
//                                                             |
ClientLinker clientLinker = new ClientLinker(this, protocol, true);
```

#### Client Inheritance PeerForP2PBase
``` C#
public class Peer : PeerForP2PBase
{
    public Peer(object peer, object publicIP, INetClient client, bool NAT):base(peer, publicIP, client, NAT)
    {
        
    }

    public override void OnGetData(SendData sendData)
    {
        //Do something when get peer data.
        //like:
        switch(sendData.Code)
        {
            case 0:
            {
                Console.WriteLine(((object[])sendData.Parameters)[1].ToString());
            }
            case 1:
            {
                Console.WriteLine(sendData.Parameters.ToString());
            }
        }
    }

    public override void OnDisconnect()
    {
        //Do something when peer disconnect.
    }
}
```

#### Set P2PAddPeer
``` C#
public class Client : ClientListen
{
    .
    .
    .
    public PeerForP2PBase P2PAddPeer(object _peer, object publicIP, INetClient client, bool NAT)
    {
        //Do something when P2P peer connect.
        return new Peer(_peer, publicIP, client, NAT);
    }
    .
    .
    .
}
```

#### Start P2P Connect
``` C#
client.clientLinker.StartP2PConnect(<peer ipendpoint>, (remote, publicremote, peer, success) =>
{
    //A delegate run on complete a connect.
});
```

#### P2P sent data
``` C#
peer.Tell(0, new object[]{ 100, "This is Data message 1" });
peer.Tell(1, "This is Data message 2");
```

#### Disconnect P2P Peer
``` C#
peer.Close();
```

## Example
### tcp
[EasyFileService](https://github.com/Jimmy01240397/EasyFileService)

### udp
[ChatService](https://github.com/Jimmy01240397/ChatService)

### udp p2p
[TestNet](https://github.com/Jimmy01240397/NetworkServicePackage/tree/master/Example/TestNet)
