using System.Net;
using System.Net.Sockets;
using Godot;
using LiteEntitySystem;
using LiteEntitySystem.Transport;
using LiteEntitySystem2DExample.Shared;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LiteEntitySystem2DExample;

public class ServerLogic : INetEventListener
{
    static ServerLogic()
    {
        Logger.LoggerImpl = new GodotLogger();
    }

    private readonly NetManager _netManager;
    private readonly NetPacketProcessor _packetProcessor;
    private readonly ulong _typesHash;
    private readonly ServerEntityManager _serverEntityManager;

    public ServerLogic()
    {
        EntityManager.RegisterFieldType<Vector2>((a, b, t) => a.Lerp(b, t));

        _netManager = new NetManager(this)
        {
            AutoRecycle = true,
            PacketPoolSize = 1000,
            SimulateLatency = true,
            SimulationMinLatency = 50,
            SimulationMaxLatency = 60,
            SimulatePacketLoss = false,
            SimulationPacketLossChance = 10
        };

        _packetProcessor = new NetPacketProcessor();
        _packetProcessor.SubscribeReusable<JoinPacket, NetPeer>(OnJoinReceived);

        var typesMap = new EntityTypesMap<GameEntities>()
            .Register(GameEntities.Player, e => new BasePlayer(e))
            .Register(GameEntities.PlayerController, e => new BasePlayerController(e));
        _typesHash = typesMap.EvaluateEntityClassDataHash();

        _serverEntityManager = ServerEntityManager.Create<PlayerInputPacket>(
            typesMap,
            (byte)PacketType.EntitySystem,
            NetworkGeneral.GameFps,
            ServerSendRate.EqualToFPS
        );
    }

    public void Start()
    {
        _netManager.Start(10515);
    }

    public void Poll()
    {
        _netManager.PollEvents();
        _serverEntityManager?.Update();
    }

    private void OnJoinReceived(JoinPacket joinPacket, NetPeer peer)
    {
        GD.Print("[S] Join packet received: " + joinPacket.UserName);

        if (joinPacket.GameHash != _typesHash)
        {
            GD.Print("[S] Client has different code");
            peer.Disconnect();
            return;
        }

        var serverPlayer = _serverEntityManager.AddPlayer(new LiteNetLibNetPeer(peer, true));
        var player = _serverEntityManager.AddEntity<BasePlayer>(e =>
        {
            e.Spawn(Vector2.Zero);
            e.Name.Value = joinPacket.UserName;
        });
        _serverEntityManager.AddController<BasePlayerController>(serverPlayer, player);
    }


    public void OnPeerConnected(NetPeer peer)
    {
        GD.Print("[S] Player connected: " + peer);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        GD.Print("[S] Player disconnected: " + disconnectInfo.Reason);

        if (peer.Tag != null)
        {
            _serverEntityManager.RemovePlayer((LiteNetLibNetPeer)peer.Tag);
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        GD.Print("[S] NetworkError: " + socketError);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber,
        DeliveryMethod deliveryMethod)
    {
        byte packetType = reader.PeekByte();
        switch ((PacketType)packetType)
        {
            case PacketType.EntitySystem:
                _serverEntityManager.Deserialize((LiteNetLibNetPeer)peer.Tag, reader.GetRemainingBytesSpan());
                break;

            case PacketType.Serialized:
                reader.GetByte();
                _packetProcessor.ReadAllPackets(reader, peer);
                break;

            default:
                GD.Print("Unhandled packet: " + packetType);
                break;
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
        UnconnectedMessageType messageType)
    {
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey("ExampleGame");
    }
}
