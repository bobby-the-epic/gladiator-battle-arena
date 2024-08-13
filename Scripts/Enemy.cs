using Godot;
using System;

public partial class Enemy : Humanoid
{
    public const float Speed = 3.0f;
    float angle;
    CharacterBody3D player;

    public override void _Ready()
    {
        base._Ready();
        player = GetNode<CharacterBody3D>("../Player");
        animationPlayer.SpeedScale = 0.75f;
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
        if (Position.DistanceTo(player.Position) > 1.5f)
        {
            Translate(Vector3.Forward * Speed * (float)delta);
        }
        else if (!attacking)
        {
            Attack();
        }
        MoveAndSlide();
    }
}
