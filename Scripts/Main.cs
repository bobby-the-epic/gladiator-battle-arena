using Godot;
using System;

public partial class Main : Node
{
    public static int Volume = 50;

    int waveNum = 0;
    int enemyCount = 0;
    bool inMainMenu = true;
    bool paused = false;
    Control pauseMenu;
    CharacterBody3D player;
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
    Timer gateTimer;
    [Export]
    AudioStreamPlayer crowdNoise;

    public override void _Ready()
    {
        gates = GetTree().GetNodesInGroup("gates");
        spawnPoints = GetTree().GetNodesInGroup("spawnPoints");

        // Signal connections
        gateTimer.Timeout += CloseGates;
        SignalBus.Instance.GameStart += OnGameStart;
        SignalBus.Instance.GladiatorDied += OnGladiatorDied;
        SignalBus.Instance.QuitToMainMenu += OnQuitToMainMenu;

        Control mainMenuNode = mainMenuScene.Instantiate<Control>();
        AddChild(mainMenuNode);
        SpawnWave();
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("pause") && !inMainMenu)
        {
            pauseMenu = pauseMenuScene.Instantiate<Control>();
            AddChild(pauseMenu);
        }
    }
    private void OnGameStart()
    {
        inMainMenu = false;
        CleanUpArena();
        player = playerScene.Instantiate<CharacterBody3D>();
        AddChild(player);
        SpawnWave();
        GetNode<Control>("MainMenu").QueueFree();
        crowdNoise.Play();
    }
    private void SpawnWave()
    {
        enemyCount += 4;
        waveNum++;
        for (int counter = 0; counter < 4; counter++)
        {
            // Spawn the gladiators at the spawn points.
            CharacterBody3D newGladiator = gladiatorScene.Instantiate<CharacterBody3D>();
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
    private void OnQuitToMainMenu()
    {
        CleanUpArena();
        player.Free();
        Control mainMenu = mainMenuScene.Instantiate<Control>();
        AddChild(mainMenu);
        Input.MouseMode = Input.MouseModeEnum.Visible;
        inMainMenu = true;
        waveNum = 0;
        SpawnWave();
        crowdNoise.Stop();
    }
}
