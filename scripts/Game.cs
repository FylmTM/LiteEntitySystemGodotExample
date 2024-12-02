using Godot;
using LiteEntitySystem2DExample.Shared;
using LiteNetLib;

namespace LiteEntitySystem2DExample;

public partial class Game : Node
{
    [Export] private Node2D _playerRoot;
    [Export] private PackedScene _playerScene;

    private ServerLogic _serverLogic;
    private ClientLogic _clientLogic;

    private Control _buttons;
    private LineEdit _ip;
    private Label _debugText;

    private bool _hostStarted = false;
    private bool _clientStarted = false;

    public override void _Ready()
    {
        var hostButton = GetNode<Button>("%Host");
        hostButton.Pressed += OnHost;
        var connectButton = GetNode<Button>("%Connect");
        connectButton.Pressed += OnConnect;

        _buttons = GetNode<Control>("%Buttons");
        _ip = GetNode<LineEdit>("%IP");
        _ip.Text = NetUtils.GetLocalIp(LocalAddrType.IPv4);
        _debugText = GetNode<Label>("%DebugText");

        GameScene.Instance = new GameScene
        {
            PlayerRoot = _playerRoot,
            PlayerScene = _playerScene
        };


        _serverLogic = new ServerLogic();
        _clientLogic = new ClientLogic(_debugText);
    }

    private void OnHost()
    {
        GD.Print("Start host");

        _buttons.Visible = false;
        _serverLogic.Start();
        _clientLogic.Connect("localhost", OnDisconnected);

        _hostStarted = true;
        _clientStarted = true;
    }

    private void OnConnect()
    {
        GD.Print("Connect to host");

        _buttons.Visible = false;
        _clientLogic.Connect(_ip.Text, OnDisconnected);

        _clientStarted = true;
    }

    private void OnDisconnected(DisconnectInfo info)
    {
        _debugText.Text = info.Reason.ToString();
    }

    public override void _Process(double delta)
    {
        if (_hostStarted)
        {
            _serverLogic.Poll();
        }

        if (_clientStarted)
        {
            _clientLogic.Poll(delta);
        }
    }
}
