using Godot;
using System;

public partial class MainMenu : Control
{
    Button backButton;
    HSlider volumeControl;

    [Export]
    Node3D cameraPivot;
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
        backButton = GetNode<Button>("OptionsMenu/BackButton");

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
        backButton.Pressed += () =>
        {
            optionsMenu.Hide();
            titleMenu.Show();
        };
        quitButton.Pressed += () => GetTree().Quit();

        Input.MouseMode = Input.MouseModeEnum.Visible;
    }
    public override void _Process(double delta)
    {
        cameraPivot.Rotate(Vector3.Up, (float)delta * .25f);
    }
}
