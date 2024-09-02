using Godot;
using System;

public partial class MainMenu : Control
{
    [Export]
    Button playButton;
    [Export]
    Button optionsButton;
    [Export]
    Button quitButton;
    public override void _Ready()
    {
        // Emit signal when the player presses the play button.
        playButton.Pressed += () =>
        {
            Hide();
            SignalBus.Instance.EmitSignal(SignalBus.SignalName.GameStart);
        };
    }
    public override void _Process(double delta)
    {
    }
}
