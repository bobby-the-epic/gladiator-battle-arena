using Godot;
using System;

public partial class MainMenu : Node
{
    [Export]
    Node3D cameraPivot;

    [Export]
    PackedScene gladiatorScene;

    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        cameraPivot.Rotate(Vector3.Up, (float)delta * .25f);
    }
}
