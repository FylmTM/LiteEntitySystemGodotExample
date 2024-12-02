using Godot;
using LiteEntitySystem;
using LiteEntitySystem.Extensions;

namespace LiteEntitySystem2DExample.Shared;

public class BasePlayer : PawnLogic
{
    [SyncVarFlags(SyncFlags.Interpolated | SyncFlags.LagCompensated)]
    private SyncVar<Vector2> _position;

    private readonly SyncVar<float> _speed;
    public readonly SyncString Name = new();

    private Vector2 _velocity;

    public Player Node;
    public Vector2 Position => _position;


    public BasePlayer(EntityParams entityParams) : base(entityParams)
    {
        _speed.Value = 100f;
    }

    protected override void OnConstructed()
    {
    }

    protected override void OnDestroy()
    {
    }

    public void Spawn(Vector2 position)
    {
        _position.Value = position;
    }

    public void SetInput(Vector2 velocity)
    {
        _velocity = velocity.Normalized() * _speed;
    }

    protected override void Update()
    {
        base.Update();
        _position.Value += _velocity * EntityManager.DeltaTimeF;
    }
}
