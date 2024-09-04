using Godot;
using System;

public partial class SignalBus : Node
{
    // Static property so that all nodes can access the signal bus without getting a reference.
    public static SignalBus Instance { get; private set; }

    [Signal]
    public delegate void DamageTakenEventHandler(int health, float angle);
    [Signal]
    public delegate void GladiatorDiedEventHandler();
    [Signal]
    public delegate void GameStartEventHandler();
    [Signal]
    public delegate void QuitToMainMenuEventHandler();

    public override void _Ready()
    {
        Instance = this;
    }
}
