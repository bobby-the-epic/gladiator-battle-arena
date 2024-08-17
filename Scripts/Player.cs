using Godot;
using System;

public partial class Player : CharacterBody3D
{
    public const float speed = 8.0f;
    public const float sprintSpeed = 12.0f;
    public const float jumpVelocity = 4.5f;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    float rotationInput;
    float tiltInput;
    float pitch;
    bool attacking = false;
    bool moving = false;
    float tiltLowerLimit = Mathf.DegToRad(-85);
    float tiltUpperLimit = Mathf.DegToRad(85);
    Vector3 mouseRotation;
    AnimationTree animTree, dupeBodyAnimTree;
    /*
     * I used a duplicate body mesh for the shadows. I wanted the camera to be able
     * to see the player's body, but I also had to do first person animations. I separated
     * the hands from the mesh with a BoneAttachment3D node and animated that.
     */

    [Export]
    float mouseSensitivity = 0.5f;
    [Export]
    Node3D cameraController;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        animTree = GetNode<AnimationTree>("AnimationTree");
        dupeBodyAnimTree = GetNode<AnimationTree>("DupeBody/AnimationTree");
        animTree.AnimationFinished += OnAnimationFinished;
        //Recording the left hand position here, just in case I need it.
        //Could be useful for a shield or bow animation.
        // Vector3 leftHandPos = new Vector3(-0.786f, -0.787f, -0.387f);
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
            dupeBodyAnimTree.Set("parameters/Walk-Idle Blend/blend_amount", 0);
            animTree.Set("parameters/Walk-Idle Blend/blend_amount", 0);
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, speed);
            dupeBodyAnimTree.Set("parameters/Walk-Idle Blend/blend_amount", 1);
            animTree.Set("parameters/Walk-Idle Blend/blend_amount", 1);
        }

        Velocity = velocity;
        MoveAndSlide();
    }
    private async void Attack()
    {
        // Plays the attack animation and waits for the AnimationFinished signal to fire.
        attacking = true;
        animTree.Set("parameters/Attack/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
        dupeBodyAnimTree.Set("parameters/Attack/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
        await ToSignal(animTree, AnimationTree.SignalName.AnimationFinished);
    }
    private void OnAnimationFinished(StringName name)
    {
        if (name == "custom/attack")
            attacking = false;
    }
}
