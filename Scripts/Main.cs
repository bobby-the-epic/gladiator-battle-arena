using Godot;
using System;

public partial class Main : Node
{
    public static int volume = -30;

    int waveNum = 0;
    int enemyCount = 0;
    bool inMainMenu = true;
    bool paused = false;
    bool gameOver = false;
    Control pauseMenu;
    Control deathMenu;
    Node3D cameraPivot;
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
    [Export]
    PackedScene deathMenuScene;
    [Export]
    PackedScene cameraPivotScene;
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
        SignalBus.Instance.PlayerDied += OnPlayerDied;

        Control mainMenuNode = mainMenuScene.Instantiate<Control>();
        AddChild(mainMenuNode);
        SpawnWave();
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        // If the pause button is pressed, the player is not in the main menu, and not dead.
        if (@event.IsActionPressed("pause") && !inMainMenu && !(bool)player.Get("dead"))
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
        waveNum = 0;
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
        enemyCount = 0;
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

        if (!gameOver)
            player.Free();
        else
            cameraPivot.QueueFree();

        Control mainMenu = mainMenuScene.Instantiate<Control>();
        AddChild(mainMenu);
        Input.MouseMode = Input.MouseModeEnum.Visible;
        inMainMenu = true;
        waveNum = 0;
        SpawnWave();
        crowdNoise.Stop();
        gameOver = false;
    }
    private void OnPlayerDied()
    {
        gameOver = true;
        player.QueueFree();
        deathMenu = deathMenuScene.Instantiate<Control>();
        cameraPivot = cameraPivotScene.Instantiate<Node3D>();
        AddChild(deathMenu);
        AddChild(cameraPivot);

        Input.MouseMode = Input.MouseModeEnum.Visible;
        // Displays how many waves the player survived.
        Label deathMenuLabel = deathMenu.GetNode<Label>("Label2");
        String waveString = "wave";
        if (waveNum > 1)
            waveString += "s";

        deathMenuLabel.Text =
            String.Format("You survived {0} {1}.\nContinue?", waveNum, waveString);
    }
}
