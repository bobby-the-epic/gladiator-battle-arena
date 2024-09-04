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
    [ExportGroup("Menus")]
    [Export]
    Control titleMenu;
    [Export]
    Control optionsMenu;

    public override void _Ready()
    {
        // Signal connections.
        playButton.Pressed += () =>
        {
            SignalBus.Instance.EmitSignal(SignalBus.SignalName.GameStart);
            QueueFree();
        };
        optionsButton.Pressed += () =>
        {
            titleMenu.Hide();
            optionsMenu.Show();
        };
        quitButton.Pressed += () => GetTree().Quit();
    }
    public override void _Process(double delta)
    {
    }
}
