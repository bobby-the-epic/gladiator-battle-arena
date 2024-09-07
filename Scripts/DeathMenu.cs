using Godot;
using System;

public partial class DeathMenu : Control
{
    [Export]
    Button restartButton;
    [Export]
    Button quitButton;

    public override void _Ready()
    {
        restartButton.Pressed += () =>
        { GD.Print("resume"); };
        quitButton.Pressed += () =>
        {
            SignalBus.Instance.EmitSignal(SignalBus.SignalName.QuitToMainMenu);
            QueueFree();
        };
    }
}
