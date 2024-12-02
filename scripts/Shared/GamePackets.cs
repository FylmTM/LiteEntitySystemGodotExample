using System;

namespace LiteEntitySystem2DExample.Shared;

public enum PacketType : byte
{
    EntitySystem,
    Serialized
}

public class JoinPacket
{
    public string UserName { get; set; }
    public ulong GameHash { get; set; }
}

[Flags]
public enum MovementKeys : byte
{
    Left = 1,
    Right = 1 << 1,
    Up = 1 << 2,
    Down = 1 << 3,
}

public struct PlayerInputPacket
{
    public MovementKeys Keys;
}
