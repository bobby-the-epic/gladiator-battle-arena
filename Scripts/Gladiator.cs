using Godot;
using System;

public partial class Gladiator : CharacterBody3D
{
    bool attacking = false;
    float angle;
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
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
        /*
         * Something with the movement logic makes the model face away from
         * the player, so I just flipped the model by 180 degrees and made 
         * the velocity formula use -lookDirection.
         */

        Vector3 velocity = Velocity;
        Vector3 lookDirection = Transform.Origin - player.Transform.Origin;
        lookDirection.Y = 0;
        lookDirection = lookDirection.Normalized();
        angle = Mathf.Atan2(lookDirection.X, lookDirection.Z);
        //Rotates the gladiator to look at the player
        Rotate(Vector3.Up, angle - Rotation.Y);

        if (Position.DistanceTo(player.Position) > 3)
        {
            animTree.Set("parameters/WalkIdleBlend/blend_amount", 0.5f);
            velocity = -lookDirection * speed;
        }
        else if (!attacking)
        {
            velocity = Vector3.Zero;
            animTree.Set("parameters/WalkIdleBlend/blend_amount", 0);
            Attack();
        }

        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        Velocity = velocity;
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
