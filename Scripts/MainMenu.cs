using Godot;
using System;

public partial class MainMenu : Control
{
    [ExportGroup("Buttons")]
    [Export]
    Button playButton;
    [Export]
    Button optionsButton;
    [Export]
    Button quitButton;
    [Export]
    Button backButton;
    [ExportGroup("Menu Screens")]
    [Export]
    Control titleScreen;
    [Export]
    Control optionsScreen;
    public override void _Ready()
    {
        // Emit signal when the player presses the play button.
        playButton.Pressed += () =>
        {
            SignalBus.Instance.EmitSignal(SignalBus.SignalName.GameStart);
            QueueFree();
        };
        optionsButton.Pressed += () =>
        {
            optionsScreen.Show();
            titleScreen.Hide();
        };
        backButton.Pressed += () =>
        {
            optionsScreen.Hide();
            titleScreen.Show();
        };
        quitButton.Pressed += () => GetTree().Quit();
    }
    public override void _Process(double delta)
    {
    }
}
