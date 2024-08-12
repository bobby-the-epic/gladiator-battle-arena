using Godot;
using System;

public partial class Enemy : CharacterBody3D
{
    public const float Speed = 5.0f;
    float angle;
    CharacterBody3D player;

    public override void _Ready()
    {
        player = GetNode<CharacterBody3D>("../Player");
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

        // Add the gravity.
        if (!IsOnFloor())
        {
            velocity += GetGravity() * (float)delta;
        }

        MoveAndSlide();
    }
}
