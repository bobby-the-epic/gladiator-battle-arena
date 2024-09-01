using Godot;
using System;

public partial class Main : Node
{
    int waveNum = 0;
    int enemyCount;
    Timer gateTimer;
    Godot.Collections.Array<Node> gates;
    Godot.Collections.Array<Node> spawnPoints;

    [Export]
    PackedScene gladiatorScene;

    // Main menu will emit the GameStart signal when the player presses the play button.
    [Signal]
    public delegate void GameStartEventHandler();
    [Signal]
    public delegate void GladiatorDeathEventHandler();

    public override void _Ready()
    {
        gateTimer = GetNode<Timer>("Timer");
        gates = GetTree().GetNodesInGroup("gates");
        spawnPoints = GetTree().GetNodesInGroup("spawnPoints");

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
            Node3D spawnPoint = (Node3D)spawnPoints[counter];
            newGladiator.Position = spawnPoint.Position;
            AddChild(newGladiator);
            // Open the gates.
            gates[counter].GetNode<AnimationPlayer>("AnimationPlayer").Play("open");
            gateTimer.Start();
        }
    }
    private void CloseGates()
    {
        for (int counter = 0; counter < gates.Count; counter++)
            gates[counter].GetNode<AnimationPlayer>("AnimationPlayer").Play("close");
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
