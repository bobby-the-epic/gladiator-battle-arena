using Godot;
using System;

public partial class CameraPivot : Node3D
{
    public override void _Process(double delta)
    {
        Rotate(Vector3.Up, (float)delta * .25f);
    }
}
