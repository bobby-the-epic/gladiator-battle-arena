using Godot;
using System;

public partial class Gladiator : CharacterBody3D
{
    float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    bool navServerReady = false;
    bool attacking = false;
    int weaponDamage = 5;
    float angle;
    const float speed = 3.0f;

    CharacterBody3D player;
    AudioStreamPlaybackPolyphonic gladiatorAudio;
    StringName walkIdleBlend = new StringName("parameters/WalkIdleBlend/blend_amount");
    StringName attackRequest = new StringName("parameters/Attack/request");
    StringName staggerRequest = new StringName("parameters/Stagger/request");
    StringName hitRequest = new StringName("parameters/Hit/request");
    StringName deathTransition = new StringName("parameters/Death State/transition_request");

    enum ANIM
    {
        IDLE,
        ATTACKING,
        MOVING,
        STAGGERED,
        DEATH
    }
    ANIM previousAnim = ANIM.IDLE;
    ANIM currentAnim = ANIM.IDLE;

    [Export]
    bool onScreen = false;
    [Export]
    bool staggered = false;
    [Export]
    bool dead = false;
    [Export]
    int health = 100;
    [Export]
    RayCast3D rayCast;
    [Export]
    VisibleOnScreenNotifier3D visibleBox;
    [Export]
    NavigationAgent3D navAgent;
    [Export]
    AnimationPlayer animPlayer;
    [Export]
    AnimationTree animTree;
    [Export]
    CharacterBody3D target;
    [ExportGroup("Audio")]
    [Export]
    AudioStreamPlayer3D gladiatorAudioPlayer;
    [Export]
    AudioStream swordHitSfx, swordSwingSfx, painSfx, deathSfx;

    [Signal]
    public delegate void HitEventHandler(int damage);
    [Signal]
    public delegate void StaggeredEventHandler();

    public override void _Ready()
    {
        player = (CharacterBody3D)GetTree().GetFirstNodeInGroup("player");
        gladiatorAudio = (AudioStreamPlaybackPolyphonic)gladiatorAudioPlayer.GetStreamPlayback();

        // If there is no player node (because the player is in the main menu).
        if (player != null)
            target = player;

        // Signal connections.
        visibleBox.ScreenEntered += () => onScreen = true;
        visibleBox.ScreenExited += () => onScreen = false;
        animTree.AnimationStarted += OnAnimationStarted;
        animTree.AnimationFinished += OnAnimationFinished;
        navAgent.VelocityComputed += OnVelocityComputed;
        Hit += OnHit;
        Staggered += () => staggered = true;
        SignalBus.Instance.PlayerDied += () => player = null;

        CallDeferred(MethodName.ActorSetup);
    }
    public override void _PhysicsProcess(double delta)
    {
        if (!navServerReady)
            return;
        SetMovement(delta);
        Animate();
    }
    public override void _ExitTree()
    {
        Hit -= OnHit;
    }
    private void SetMovement(double delta)
    {
        if (dead || target == null)
            return;

        if (player == null || (bool)target.Get("dead") == true || target == null)
            FindNewTarget();

        // Sets the target of the navigation agent.
        navAgent.TargetPosition = target.GlobalPosition;
        Vector3 velocity = Vector3.Zero;
        Vector3 lookDirection = GlobalPosition.DirectionTo(navAgent.GetNextPathPosition());
        lookDirection.Y = 0;
        angle = Mathf.Atan2(lookDirection.X, lookDirection.Z);
        // Rotates the gladiator to look at the target.
        Rotate(Vector3.Up, angle - Rotation.Y);

        if (!navAgent.IsNavigationFinished() && !staggered)
        {
            velocity = lookDirection * speed;
        }
        else if (!attacking && !staggered)
        {
            velocity = Vector3.Zero;
            attacking = true;
            Attack();
        }

        // Adds gravity just in case
        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        navAgent.Velocity = velocity;
    }
    private void Animate()
    {
        if (dead)
            currentAnim = ANIM.DEATH;
        else if (staggered)
            currentAnim = ANIM.STAGGERED;
        else if (attacking)
            currentAnim = ANIM.ATTACKING;
        else
            currentAnim = ANIM.MOVING;

        if (currentAnim != previousAnim)
        {
            switch (currentAnim)
            {
                case ANIM.MOVING:
                    animTree.Set(walkIdleBlend, 0.5f);
                    break;
                case ANIM.ATTACKING:
                    animTree.Set(walkIdleBlend, 0);
                    Attack();
                    break;
                case ANIM.STAGGERED:
                    animTree.Set(attackRequest, (int)AnimationNodeOneShot.OneShotRequest.Abort);
                    animTree.Set(staggerRequest, (int)AnimationNodeOneShot.OneShotRequest.Fire);
                    break;
                case ANIM.DEATH:
                    animTree.Set(deathTransition, "Dead");
                    GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
                    break;
            }
        }

        previousAnim = currentAnim;
    }
    private async void Attack()
    {
        animTree.Set(attackRequest, (int)AnimationNodeOneShot.OneShotRequest.Fire);
        await ToSignal(animTree, AnimationTree.SignalName.AnimationFinished);
    }
    private async void ActorSetup()
    {
        // Wait for the first physics frame so the NavigationServer can sync.
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        navServerReady = true;
        if (player != null)
            return;

        // Get a random gladiator and assign it as a target.
        Godot.Collections.Array<Node> gladiators = GetTree().GetNodesInGroup("gladiators");
        Random randomNum = new Random();
        CharacterBody3D potentialTarget;
        potentialTarget = (CharacterBody3D)gladiators[randomNum.Next(gladiators.Count)];
        while (potentialTarget == this)
            potentialTarget = (CharacterBody3D)gladiators[randomNum.Next(gladiators.Count)];
        target = potentialTarget;
    }
    private void FindNewTarget()
    {
        // Gets a target in the gladiator group that is not this and not dead.
        Godot.Collections.Array<Node> gladiators = GetTree().GetNodesInGroup("gladiators");
        for (int counter = 0; counter < gladiators.Count; counter++)
        {
            CharacterBody3D potentialTarget = (CharacterBody3D)gladiators[counter];
            if (potentialTarget == this || (bool)potentialTarget.Get("dead"))
                continue;
            else
            {
                target = potentialTarget;
                break;
            }
        }
    }
    private async void OnAnimationStarted(StringName animName)
    {
        if (animName == "idle")
        {
            staggered = true;
            attacking = false;
            await ToSignal(animTree, AnimationTree.SignalName.AnimationFinished);
        }
    }
    private void OnAnimationFinished(StringName animName)
    {
        if (animName == "custom/attack")
        {
            attacking = false;
            // If the target is in the range of the ray, then damage the target.
            if (rayCast.IsColliding())
            {
                if (target == player)
                    target.EmitSignal(Player.SignalName.Hit, weaponDamage, this);
                else
                {
                    target.EmitSignal(Gladiator.SignalName.Hit, weaponDamage);
                }
                gladiatorAudio.PlayStream(swordHitSfx, volumeDb: Main.volume);
            }
            else
                gladiatorAudio.PlayStream(swordSwingSfx, volumeDb: Main.volume + 10);
        }
        else if (animName == "idle")
        {
            staggered = false;
        }
    }
    private void OnVelocityComputed(Vector3 safeVelocity)
    {
        Velocity = safeVelocity;
        if (dead)
            Velocity = Vector3.Zero;
        MoveAndSlide();
    }
    private void OnHit(int damage)
    {
        if (!dead)
        {
            health -= damage;
            if (health <= 0)
            {
                dead = true;
                navAgent.AvoidanceEnabled = false;
                SignalBus.Instance.EmitSignal(SignalBus.SignalName.GladiatorDied);
                Velocity = Vector3.Zero;
                gladiatorAudio.PlayStream(deathSfx, volumeDb: Main.volume + 10);
                return;
            }
            gladiatorAudio.PlayStream(painSfx, volumeDb: Main.volume);
            animTree.Set(hitRequest, (int)AnimationNodeOneShot.OneShotRequest.Fire);
        }
    }
}
