using Godot;
using System;

public partial class PauseMenu : Control
{
    [ExportGroup("Buttons")]
    [Export]
    Button resumeButton;
    [Export]
    Button optionsButton;
    [Export]
    Button quitButton;
    [ExportGroup("")]
    [Export]
    PackedScene optionsMenuScene;
    public override void _Ready()
    {
        // Signal connections.

        // Resume the game.
        resumeButton.Pressed += () =>
        {
            GetTree().Paused = false;
            QueueFree();
        };
        // Show the options menu.
        optionsButton.Pressed += () =>
        {
            Hide();
            Control optionsMenu = optionsMenuScene.Instantiate<Control>();
            optionsMenu.Show();
        };
        quitButton.Pressed += () =>
        {
            // Quit to main menu.
        };

        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetTree().Paused = true;
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("pause"))
        {
            QueueFree();
        }
    }
    public override void _ExitTree()
    {
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }
}
