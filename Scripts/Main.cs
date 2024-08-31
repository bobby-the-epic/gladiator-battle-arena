using Godot;
using System;

public partial class Main : Node
{

    int waveNum = 0;
    int enemyCount;
    Node3D[] spawnPoints;
    AnimationPlayer[] gateAnimPlayers;
    Timer gateTimer;

    // Exported fields so I don't have to keep calling GetNode in _Ready function.

    [Export]
    PackedScene gladiatorScene;

    [ExportGroup("Spawn Points")]
    [Export]
    Node3D spawnPoint1;
    [Export]
    Node3D spawnPoint2;
    [Export]
    Node3D spawnPoint3;
    [Export]
    Node3D spawnPoint4;

    // Main menu will emit the GameStart signal when the player presses the play button.
    [Signal]
    public delegate void GameStartEventHandler();
    [Signal]
    public delegate void GladiatorDeathEventHandler();

    public override void _Ready()
    {
        gateTimer = GetNode<Timer>("Timer");
        spawnPoints = new Node3D[] { spawnPoint1, spawnPoint2, spawnPoint3, spawnPoint4 };
        gateAnimPlayers = new AnimationPlayer[4];
        for (int counter = 0; counter < gateAnimPlayers.Length; counter++)
        {
            gateAnimPlayers[counter] = GetNode<AnimationPlayer>(
                String.Format("World/ArenaLevel/Gates/Gate{0}/AnimationPlayer", counter + 1));
        }

        // Signal connections
        gateTimer.Timeout += CloseGates;
        GameStart += () => SpawnWave();
        GladiatorDeath += () =>
        {
            enemyCount--;
            if (enemyCount == 0)
            {
                CleanUpArena();
                SpawnWave();
            }
        };
        EmitSignal("GameStart");
    }
    public override void _Process(double delta)
    {
    }
    private void SpawnWave()
    {
        enemyCount = 4;
        waveNum++;
        for (int counter = 0; counter < 4; counter++)
        {
            // Spawn the gladiators at the spawn points.
            CharacterBody3D newGladiator = gladiatorScene.Instantiate() as CharacterBody3D;
            newGladiator.Position = spawnPoints[counter].Position;
            AddChild(newGladiator);
            // Open the gates.
            gateAnimPlayers[counter].Play("open");
            gateTimer.Start();
        }
    }
    private void CloseGates()
    {
        for (int counter = 0; counter < 4; counter++)
        {
            gateAnimPlayers[counter].Play("close");
        }
    }
    private void CleanUpArena()
    {
        // Gets all of the gladiators in the arena (because they are all in the "gladiators group")
        // and deletes them.
        Godot.Collections.Array<Node> deadGladiators = GetTree().GetNodesInGroup("gladiators");
        for (int counter = 0; counter < deadGladiators.Count; counter++)
        {
            deadGladiators[counter].QueueFree();
        }
    }
}
