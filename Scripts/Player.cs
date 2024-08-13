using Godot;
using System;

public partial class Player : Humanoid
{
    public const float speed = 8.0f;
    public const float sprintSpeed = 12.0f;
    public const float jumpVelocity = 4.5f;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    float rotationInput;
    float tiltInput;
    float pitch;
    Vector3 mouseRotation;
    [Export]
    float mouseSensitivity = 0.5f;
    [Export]
    float tiltLowerLimit = Mathf.DegToRad(-90);
    [Export]
    float tiltUpperLimit = Mathf.DegToRad(90);
    [Export]
    Node3D cameraController;

    public override void _Ready()
    {
        base._Ready();
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }
    public override void _PhysicsProcess(double delta)
    {
        SetMovement(delta);
        UpdateCamera(delta);
    }
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("attack") && !attacking)
            Attack();
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseEvent && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            rotationInput = -mouseEvent.Relative.X * mouseSensitivity;
            tiltInput = -mouseEvent.Relative.Y * mouseSensitivity;
        }
    }
    private void UpdateCamera(double delta)
    {
        pitch += tiltInput * (float)delta;
        pitch = Mathf.Clamp(pitch, tiltLowerLimit, tiltUpperLimit);

        // Rotate the player node around the Y-axis for horizontal camera movement
        Rotate(Vector3.Up, rotationInput * (float)delta);

        // Rotate the camera controller node around the X-axis for vertical camera movement
        cameraController.Rotation = new Vector3(pitch, cameraController.Rotation.Y, cameraController.Rotation.Z);

        rotationInput = 0.0f;
        tiltInput = 0.0f;
    }
    private void SetMovement(double delta)
    {
        Vector3 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        // Handle Jump.
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            velocity.Y = jumpVelocity;

        Vector2 inputDir = Input.GetVector("moveLeft", "moveRight", "moveForward", "moveBackward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            if (Input.IsPhysicalKeyPressed(Key.Shift))
            {
                velocity.X = direction.X * sprintSpeed;
                velocity.Z = direction.Z * sprintSpeed;
            }
            else
            {
                velocity.X = direction.X * speed;
                velocity.Z = direction.Z * speed;
            }
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, speed);
        }

        Velocity = velocity;
        MoveAndSlide();
    }
}
