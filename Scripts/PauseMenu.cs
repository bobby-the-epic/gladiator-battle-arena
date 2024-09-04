using Godot;
using System;

public partial class PauseMenu : Control
{
    Button backButton;
    HSlider volumeControl;

    [Export]
    Control menu;
    [Export]
    Control optionsMenu;
    [ExportGroup("Buttons")]
    [Export]
    Button resumeButton;
    [Export]
    Button optionsButton;
    [Export]
    Button quitButton;

    public override void _Ready()
    {
        backButton = GetNode<Button>("OptionsMenu/BackButton");
        volumeControl = GetNode<HSlider>("OptionsMenu/HSlider");
        volumeControl.Value = Main.Volume;
        // Signal connections.

        // Resume the game.
        resumeButton.Pressed += () =>
        {
            GetTree().Paused = false;
            Input.MouseMode = Input.MouseModeEnum.Captured;
            QueueFree();
        };
        // Show the options menu.
        optionsButton.Pressed += () =>
        {
            menu.Hide();
            optionsMenu.Show();
        };
        backButton.Pressed += () =>
        {
            optionsMenu.Hide();
            menu.Show();
        };
        // Quit to main menu.
        quitButton.Pressed += () =>
        {
            SignalBus.Instance.EmitSignal(SignalBus.SignalName.QuitToMainMenu);
            QueueFree();
        };
        volumeControl.ValueChanged += (double volume) => Main.Volume = (int)volume;

        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetTree().Paused = true;
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("pause"))
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            QueueFree();
        }
    }
    public override void _ExitTree()
    {
        GetTree().Paused = false;
    }
}
