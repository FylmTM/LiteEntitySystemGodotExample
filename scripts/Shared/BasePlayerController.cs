using System.Diagnostics.CodeAnalysis;
using Godot;
using LiteEntitySystem;

namespace LiteEntitySystem2DExample.Shared;

public class BasePlayerController : HumanControllerLogic<PlayerInputPacket, BasePlayer>
{
    private PlayerInputPacket _nextCommand;

    public BasePlayerController(EntityParams entityParams) : base(entityParams)
    {
    }

    protected override void VisualUpdate()
    {
        if (ControlledEntity == null)
            return;

        //input
        Vector2 velocity = new Vector2(Input.GetAxis("ui_left", "ui_right"), Input.GetAxis("ui_up", "ui_down"));

        GD.Print(velocity);

        if (velocity.X < -0.5f)
            _nextCommand.Keys |= MovementKeys.Left;
        if (velocity.X > 0.5f)
            _nextCommand.Keys |= MovementKeys.Right;

        if (velocity.Y < -0.5f)
            _nextCommand.Keys |= MovementKeys.Up;
        if (velocity.Y > 0.5f)
            _nextCommand.Keys |= MovementKeys.Down;
    }

    protected override void ReadInput(in PlayerInputPacket input)
    {
        var velocity = Vector2.Zero;

        if (input.Keys.HasFlagFast(MovementKeys.Up))
            velocity.Y = -1f;
        if (input.Keys.HasFlagFast(MovementKeys.Down))
            velocity.Y = 1f;

        if (input.Keys.HasFlagFast(MovementKeys.Left))
            velocity.X = -1f;
        if (input.Keys.HasFlagFast(MovementKeys.Right))
            velocity.X = 1f;

        ControlledEntity?.SetInput(velocity);
    }

    protected override void GenerateInput([UnscopedRef] out PlayerInputPacket input)
    {
        input = _nextCommand;
        _nextCommand.Keys = 0;
    }
}
