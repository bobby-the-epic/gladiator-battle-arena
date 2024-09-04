using Godot;
using System;

public partial class Main : Node
{
    int waveNum = 0;
    int enemyCount = 0;
    bool inMainMenu = true;
    Godot.Collections.Array<Node> gates;
    Godot.Collections.Array<Node> spawnPoints;

    [ExportGroup("Scenes")]
    [Export]
    PackedScene gladiatorScene;
    [Export]
    PackedScene playerScene;
    [Export]
    PackedScene mainMenuScene;
    [Export]
    PackedScene pauseMenuScene;
    [ExportGroup("")]
    [Export]
    Node3D cameraPivot;
    [Export]
    Timer gateTimer;

    public override void _Ready()
    {
        gates = GetTree().GetNodesInGroup("gates");
        spawnPoints = GetTree().GetNodesInGroup("spawnPoints");

        // Signal connections
        gateTimer.Timeout += CloseGates;
        SignalBus.Instance.GameStart += OnGameStart;
        SignalBus.Instance.GladiatorDied += OnGladiatorDied;

        Node mainMenuNode = mainMenuScene.Instantiate();
        AddChild(mainMenuNode);
        SpawnWave();
    }
    public override void _Process(double delta)
    {
        cameraPivot.Rotate(Vector3.Up, (float)delta * .25f);
    }
    private void OnGameStart()
    {
        inMainMenu = false;
        CleanUpArena();
        CharacterBody3D player = (CharacterBody3D)playerScene.Instantiate();
        AddChild(player);
        cameraPivot.GetNode<Camera3D>("Camera3D").Current = false;
        SpawnWave();
        GetNode<Control>("MainMenu").QueueFree();
    }
    private void SpawnWave()
    {
        enemyCount += 4;
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
        Godot.Collections.Array<Node> gladiators = GetTree().GetNodesInGroup("gladiators");
        for (int counter = 0; counter < gladiators.Count; counter++)
            gladiators[counter].QueueFree();
    }
    private void OnGladiatorDied()
    {
        enemyCount--;
        if (!inMainMenu && enemyCount == 0)
        {
            CleanUpArena();
            SpawnWave();
        }
        else if (inMainMenu && enemyCount <= 1)
            SpawnWave();
    }
}
