using Godot;
using System;

public partial class OptionsMenu : Control
{
    Godot.Collections.Array<Node> audioStreams;
    AudioStreamPlayer[] audioStreamPlayers;
    Node player;

    [Export]
    HSlider volumeControl;
    [Export]
    HSlider sensitivityControl;

    public override void _Ready()
    {
        player = GetTree().GetFirstNodeInGroup("player");
        if (player != null)
            sensitivityControl.Value = (double)player.Get("mouseSensitivity");
        volumeControl.Value = Main.volume;

        // Get the audio stream players in the audio group
        audioStreams = GetTree().GetNodesInGroup("audio");
        audioStreamPlayers = new AudioStreamPlayer[audioStreams.Count];
        for (int counter = 0; counter < audioStreams.Count; counter++)
        {
            audioStreamPlayers[counter] = (AudioStreamPlayer)audioStreams[counter];
        }

        volumeControl.ValueChanged += OnVolumeChanged;
        sensitivityControl.ValueChanged += OnSensitivityChanged;
        for (int counter = 0; counter < audioStreams.Count; counter++)
            audioStreamPlayers[counter].VolumeDb = Main.volume;
    }
    private void OnVolumeChanged(double volume)
    {
        // Change the volume in all the audio streams when the volume slider is changed.
        for (int counter = 0; counter < audioStreams.Count; counter++)
            audioStreamPlayers[counter].VolumeDb = (float)volume;
        Main.volume = (int)volume;
    }
    private void OnSensitivityChanged(double sensitivity)
    {
        if (player != null)
            player.Set("mouseSensitivity", (float)sensitivity);
    }
}
