using System;
using System.Net;
using System.Net.Sockets;
using Godot;
using LiteEntitySystem;
using LiteEntitySystem.Transport;
using LiteEntitySystem2DExample.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using Environment = Godot.Environment;

namespace LiteEntitySystem2DExample;

public class ClientLogic : INetEventListener
{
    static ClientLogic()
    {
        LiteEntitySystem.Logger.LoggerImpl = new GodotLogger();
    }

    public static ClientLogic Instance { get; private set; }

    private Action<DisconnectInfo> _onDisconnected;

    private NetManager _netManager;
    private NetDataWriter _writer;
    private NetPacketProcessor _packetProcessor;

    private Label _debugText;
    private string _userName;
    private NetPeer _server;
    private ClientEntityManager _entityManager;
    private int _ping;

    private int PacketsInPerSecond;
    private int BytesInPerSecond;
    private int PacketsOutPerSecond;
    private int BytesOutPerSecond;

    private double _secondTimer;
    private BasePlayer _ourPlayer;

    public ClientLogic(Label debugText)
    {
        EntityManager.RegisterFieldType<Vector2>((a, b, t) => a.Lerp(b, t));

        Instance = this;
        _debugText = debugText;
        _userName = "Godot " + GD.RandRange(0, 100000);

        _writer = new NetDataWriter();
        _packetProcessor = new NetPacketProcessor();
        _netManager = new NetManager(this)
        {
            AutoRecycle = true,
            EnableStatistics = true,
            IPv6Enabled = false,
            SimulateLatency = true,
            SimulationMinLatency = 50,
            SimulationMaxLatency = 60,
            SimulatePacketLoss = false,
            SimulationPacketLossChance = 10
        };
        _netManager.Start();
    }

    public void Poll(double delta)
    {
        _netManager.PollEvents();
        _secondTimer += delta;

        if (_secondTimer >= 1f)
        {
            _secondTimer -= 1f;
            var stats = _netManager.Statistics;
            BytesInPerSecond = (int)stats.BytesReceived;
            PacketsInPerSecond = (int)stats.PacketsReceived;
            BytesOutPerSecond = (int)stats.BytesSent;
            PacketsOutPerSecond = (int)stats.PacketsSent;
            stats.Reset();
        }

        if (_entityManager != null)
        {
            _entityManager.Update();
            _debugText.Text = $@"
C_ServerTick: {_entityManager.ServerTick}
C_Tick: {_entityManager.Tick}
C_LPRCS: {_entityManager.LastProcessedTick}
C_StoredCommands: {_entityManager.StoredCommands}
C_Entities: {_entityManager.EntitiesCount}
C_ServerInputBuffer: {_entityManager.ServerInputBuffer}
C_LerpBufferCount: {_entityManager.LerpBufferCount}
C_LerpBufferTime: {_entityManager.LerpBufferTimeLength}
Jitter: {_entityManager.NetworkJitter}
Ping: {_ping}
IN: {BytesInPerSecond / 1000f} KB/s({PacketsInPerSecond})
OUT: {BytesOutPerSecond / 1000f} KB/s({PacketsOutPerSecond})
PendingRemove: {_entityManager.PendingToRemoveEntites}";
            GD.Print(_debugText.Text);
        }
        else
        {
            _debugText.Text = "Disconnected";
        }
    }

    private void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new()
    {
        if (_server == null)
            return;
        _writer.Reset();
        _writer.Put((byte)PacketType.Serialized);
        _packetProcessor.Write(_writer, packet);
        _server.Send(_writer, deliveryMethod);
    }

    public void OnPeerConnected(NetPeer peer)
    {
        GD.Print("[C] Connected to server: " + peer);
        _server = peer;

        var typesMap = new EntityTypesMap<GameEntities>()
            .Register(GameEntities.Player, e => new BasePlayer(e))
            .Register(GameEntities.PlayerController, e => new BasePlayerController(e));

        SendPacket(new JoinPacket { UserName = _userName, GameHash = typesMap.EvaluateEntityClassDataHash() },
            DeliveryMethod.ReliableOrdered);

        _entityManager = ClientEntityManager.Create<PlayerInputPacket>(
            typesMap,
            new LiteNetLibNetPeer(peer, true),
            (byte)PacketType.EntitySystem,
            NetworkGeneral.GameFps);

        _entityManager.GetEntities<BasePlayer>().SubscribeToConstructed(player =>
        {
            var node = GameScene.Instance.PlayerScene.Instantiate<Player>();
            node.Name = $"Player_{player.Name}_{player.Id}";
            node.Entity = player;

            GameScene.Instance.PlayerRoot.AddChild(node);
        }, true);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _server = null;
        _entityManager = null;
        GD.Print("[C] Disconnected from server: " + disconnectInfo.Reason);
        if (_onDisconnected != null)
        {
            _onDisconnected(disconnectInfo);
            _onDisconnected = null;
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        GD.Print("[C] NetworkError: " + socketError);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber,
        DeliveryMethod deliveryMethod)
    {
        byte packetType = reader.PeekByte();
        var pt = (PacketType)packetType;
        switch (pt)
        {
            case PacketType.EntitySystem:
                _entityManager.Deserialize(reader.GetRemainingBytesSpan());
                break;

            case PacketType.Serialized:
                reader.GetByte();
                _packetProcessor.ReadAllPackets(reader);
                break;

            default:
                GD.Print("Unhandled packet: " + pt);
                break;
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
        UnconnectedMessageType messageType)
    {
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        _ping = latency;
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        request.Reject();
    }

    public void Connect(string ip, Action<DisconnectInfo> onDisconnected)
    {
        _onDisconnected = onDisconnected;
        _netManager.Connect(ip, 10515, "ExampleGame");
    }
}
