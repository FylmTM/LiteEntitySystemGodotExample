using Godot;
using LiteEntitySystem;

namespace LiteEntitySystem2DExample.Shared;

public class GodotLogger : ILogger
{
    public void Log(string log)
    {
        GD.Print("[LES] " + log + "");
    }

    public void LogWarning(string log)
    {
        GD.Print("[LES:Warn] " + log);
    }

    public void LogError(string log)
    {
        GD.PrintErr("[LES:Error] " + log);
    }
}
