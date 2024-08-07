using Godot;
using System;

public partial class Player : CharacterBody3D
{
    public const float Speed = 5.0f;
    public const float JumpVelocity = 4.5f;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    bool mouseInput = false;
    float rotationInput;
    float tiltInput;
    float pitch;
    Vector3 mouseRotation;
    [Export]
    float tiltLowerLimit = Mathf.DegToRad(-90);
    [Export]
    float tiltUpperLimit = Mathf.DegToRad(90);
    [Export]
    Node3D cameraController;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }
    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        UpdateCamera(delta);

        // Handle Jump.
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            velocity.Y = JumpVelocity;

        Vector2 inputDir = Input.GetVector("moveLeft", "moveRight", "moveForward", "moveBackward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseEvent && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            mouseInput = true;
            rotationInput = -mouseEvent.Relative.X;
            tiltInput = -mouseEvent.Relative.Y;
        }
    }
    public void UpdateCamera(double delta)
    {
        mouseRotation.X += tiltInput * (float)delta;
        mouseRotation.X = Mathf.Clamp(mouseRotation.X, tiltLowerLimit, tiltUpperLimit);
        mouseRotation.Y += rotationInput * (float)delta;
        pitch += tiltInput * (float)delta;
        pitch = Mathf.Clamp(pitch, tiltLowerLimit, tiltUpperLimit);

        // Rotate the player node around the Y-axis for horizontal camera movement
        Rotate(Vector3.Up, rotationInput * (float)delta);

        // Rotate the camera controller node around the X-axis for vertical camera movement
        cameraController.Rotation = new Vector3(pitch, cameraController.Rotation.Y, cameraController.Rotation.Z);

        rotationInput = 0.0f;
        tiltInput = 0.0f;
    }
}
