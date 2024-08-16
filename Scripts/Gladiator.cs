using Godot;
using System;

public partial class Gladiator : CharacterBody3D
{
    bool attacking = false;
    float angle;
    const float speed = 3.0f;
    AnimationTree animTree;
    AnimationPlayer animPlayer;
    CharacterBody3D player;

    public override void _Ready()
    {
        player = GetNode<CharacterBody3D>("../Player");
        animTree = GetNode<AnimationTree>("AnimationTree");
        animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        animTree.AnimationFinished += OnAnimationFinished;
    }
    public override void _PhysicsProcess(double delta)
    {
        SetMovement(delta);
    }
    private void SetMovement(double delta)
    {
        Vector3 velocity = Velocity;
        Vector3 lookDirection = Transform.Origin - player.Transform.Origin;
        lookDirection.Y = 0;
        lookDirection = lookDirection.Normalized();
        angle = Mathf.Atan2(lookDirection.X, lookDirection.Z);
        Rotate(Vector3.Up, angle - Rotation.Y);
        if (Position.DistanceTo(player.Position) > 2)
        {
            animTree.Set("parameters/WalkIdleBlend/blend_amount", 0.5f);
            Translate(Vector3.Forward * speed * (float)delta);
        }
        else if (!attacking)
        {
            animTree.Set("parameters/WalkIdleBlend/blend_amount", 0);
            Attack();
        }
        MoveAndSlide();
    }
    private async void Attack()
    {
        attacking = true;
        animTree.Set("parameters/OneShot/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
        await ToSignal(animTree, AnimationPlayer.SignalName.AnimationFinished);
    }
    private void OnAnimationFinished(StringName animName)
    {
        if (animName == "custom/attack")
            attacking = false;

    }
}
