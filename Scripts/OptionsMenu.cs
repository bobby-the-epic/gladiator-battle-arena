using Godot;
using System;

public partial class OptionsMenu : Control
{
    Godot.Collections.Array<Node> audioStreams;
    AudioStreamPlayer[] audioStreamPlayers;

    [Export]
    HSlider volumeControl;

    public override void _Ready()
    {
        volumeControl.Value = Main.volume;
        // Get the audio stream players in the audio group
        audioStreams = GetTree().GetNodesInGroup("audio");
        audioStreamPlayers = new AudioStreamPlayer[audioStreams.Count];
        for (int counter = 0; counter < audioStreams.Count; counter++)
        {
            audioStreamPlayers[counter] = (AudioStreamPlayer)audioStreams[counter];
        }
        volumeControl.ValueChanged += OnVolumeChanged;
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
}
