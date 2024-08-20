using Godot;
using System;

public partial class Gladiator : CharacterBody3D
{
    bool navServerReady = false;
    bool attacking = false;
    float angle;
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    const float speed = 3.0f;
    AnimationTree animTree;
    AnimationPlayer animPlayer;
    CharacterBody3D player;
    NavigationAgent3D navAgent;
    Area3D attackRange;

    public override void _Ready()
    {
        player = GetNode<CharacterBody3D>("/root/Main/Player");
        navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
        navAgent.VelocityComputed += OnVelocityComputed;
        animTree = GetNode<AnimationTree>("AnimationTree");
        animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        animTree.AnimationFinished += OnAnimationFinished;
        attackRange = GetNode<Area3D>("Area3D");
        CallDeferred(MethodName.ActorSetup);
    }
    public override void _PhysicsProcess(double delta)
    {
        if (!navServerReady)
            return;
        SetMovement(delta);
    }
    private void SetMovement(double delta)
    {
        navAgent.TargetPosition = player.GlobalPosition;
        Vector3 velocity = Velocity;
        Vector3 lookDirection = GlobalPosition.DirectionTo(navAgent.GetNextPathPosition());
        lookDirection.Y = 0;
        angle = Mathf.Atan2(lookDirection.X, lookDirection.Z);
        //Rotates the gladiator to look at the player
        Rotate(Vector3.Up, angle - Rotation.Y);

        if (!navAgent.IsNavigationFinished())
        {
            animTree.Set("parameters/WalkIdleBlend/blend_amount", 0.5f);
            velocity = lookDirection * speed;
        }
        else if (!attacking)
        {
            velocity = Vector3.Zero;
            animTree.Set("parameters/WalkIdleBlend/blend_amount", 0);
            Attack();
        }

        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        navAgent.Velocity = velocity;
    }
    private async void Attack()
    {
        attacking = true;
        animTree.Set("parameters/OneShot/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
        await ToSignal(animTree, AnimationTree.SignalName.AnimationFinished);
    }
    private async void ActorSetup()
    {
        // Wait for the first physics frame so the NavigationServer can sync.
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        navServerReady = true;
    }
    private void OnAnimationFinished(StringName animName)
    {
        if (animName == "custom/attack")
        {
            attacking = false;
            //If the player is in the range of attack, then damage the player
            if (attackRange.HasOverlappingBodies())
                player.EmitSignal(Player.SignalName.Hit, 5);
        }
    }
    private void OnVelocityComputed(Vector3 safeVelocity)
    {
        Velocity = safeVelocity;
        MoveAndSlide();
    }
}
