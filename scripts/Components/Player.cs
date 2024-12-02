using Godot;
using LiteEntitySystem2DExample.Shared;

namespace LiteEntitySystem2DExample;

public partial class Player : Node2D
{
    public BasePlayer Entity;

    public override void _Process(double delta)
    {
        Position = Entity.Position;
    }
}
