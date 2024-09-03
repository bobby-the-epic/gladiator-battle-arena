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
    public CharacterBody3D target;

    [Signal]
    public delegate void HitEventHandler(int damage);
    [Signal]
    public delegate void StaggeredEventHandler();

    public override void _Ready()
    {
        player = (CharacterBody3D)GetTree().GetFirstNodeInGroup("player");
        // If there is no player node (because the player is in the main menu).
        if (player == null)
        {
            // Extends the gladiator's attack range to reach other gladiators.
            rayCast.TargetPosition += Vector3.Back;
        }
        else
            target = player;

        // Signal connections.
        visibleBox.ScreenEntered += () => onScreen = true;
        visibleBox.ScreenExited += () => onScreen = false;
        animTree.AnimationStarted += OnAnimationStarted;
        animTree.AnimationFinished += OnAnimationFinished;
        navAgent.VelocityComputed += OnVelocityComputed;
        Hit += OnHit;
        Staggered += () => staggered = true;

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
        if (dead)
            return;

        if (target == null || (bool)target.Get("dead") == true)
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
    }
    private void FindNewTarget()
    {
        Godot.Collections.Array<Node> gladiators = GetTree().GetNodesInGroup("gladiators");
        // Generate a random number.
        Random rng = new Random();
        target = (CharacterBody3D)gladiators[rng.Next(gladiators.Count)];
        if (target == this)
            FindNewTarget();
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
            }
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
                return;
            }
            GD.Print(Name + " has taken " + damage + " damage.");
            animTree.Set(hitRequest, (int)AnimationNodeOneShot.OneShotRequest.Fire);
        }
    }
}
