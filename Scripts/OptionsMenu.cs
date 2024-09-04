using Godot;
using System;

public partial class OptionsMenu : Control
{
    [Export]
    Button backButton;
    public override void _Ready()
    {
        backButton.Pressed += () =>
        {
            Hide();
            // Gets the first child of the parent node.
            // This is for getting either the main menu or pause menu.
            // So this node needs to load last.
            GetParent<Control>().GetChild<Control>(0).Show();
        };
    }
}
