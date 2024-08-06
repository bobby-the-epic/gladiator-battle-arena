using Godot;
using System;

public partial class Main : Node
{
    Node world;
    Node level1 = ResourceLoader.Load<PackedScene>("res://level1.tscn").Instantiate();
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // world = GetNode("World");
        // world.AddChild(level1);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
