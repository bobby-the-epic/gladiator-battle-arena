using Godot;
using System;

public partial class Player : CharacterBody3D
{
    public const float speed = 8.0f;
    public const float sprintSpeed = 12.0f;
    public const float jumpVelocity = 4.5f;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    public int health = 100;
    public bool dead = false;

    float rotationInput;
    float tiltInput;
    float pitch;
    float tiltLowerLimit = Mathf.DegToRad(-85);
    float tiltUpperLimit = Mathf.DegToRad(85);
    Vector3 mouseRotation;
    AnimationTree animTree, dupeBodyAnimTree;
    Area3D attackRange;
    Timer attackCooldown;
    // Animation parameter StringNames for more readable code
    StringName walkBlend = new StringName("parameters/Walk Blend/blend_amount");
    StringName jumpRequest = new StringName("parameters/Jump/request");
    StringName attackRequest = new StringName("parameters/Attack/request");

    enum ANIM
    {
        IDLE,
        MOVING,
        JUMPING,
        MOVINGANDJUMPING
    }
    ANIM previousAnim = ANIM.IDLE;
    ANIM newAnim = ANIM.IDLE;

    /*
     * I used a duplicate body mesh for the shadows. I wanted the camera to be
     * able to see the player's body, but I also had to do first person
     * animations. I separated the hands from the mesh with a BoneAttachment3D
     * node and animated that.
     */

    [Export]
    bool attacking = false;
    [Export]
    bool blocking = false;
    [Export]
    bool blockShove = false;
    [Export]
    float mouseSensitivity = 0.5f;
    [Export]
    Camera3D cameraController;

    [Signal]
    public delegate void HitEventHandler(int damage);

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        animTree = GetNode<AnimationTree>("AnimationTree");
        dupeBodyAnimTree = GetNode<AnimationTree>("DupeBody/AnimationTree");
        attackRange = GetNode<Area3D>("Camera3D/Area3D");
        attackCooldown = GetNode<Timer>("AttackCooldown");
        attackCooldown.Timeout += OnTimerTimeout;
        animTree.AnimationFinished += OnAnimationFinished;
        Hit += OnHit;

        // Recording the left hand position here, just in case I need it.
        // Could be useful for a shield or bow animation.
        // Vector3 leftHandPos = new Vector3(-0.786f, -0.787f, -0.387f);
    }
    public override void _PhysicsProcess(double delta)
    {
        SetMovement(delta);
        Animate();
        UpdateCamera(delta);
    }
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("attack") && !attacking && !blocking)
            Attack();
        // The animTree detects the blocking bool and plays the animations in its state machine.
        if (Input.IsActionJustPressed("block"))
            blocking = true;
        if (Input.IsActionJustReleased("block"))
            blocking = false;
        if (blocking && Input.IsActionJustPressed("attack"))
        {
            blockShove = true;
        }
    }
    public override void _Input(InputEvent @event)
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
        Vector2 inputDir = Input.GetVector("moveLeft", "moveRight", "moveForward", "moveBackward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        // Handle Jump.
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            velocity.Y = jumpVelocity;

        if (direction != Vector3.Zero)
        {
            if (Input.IsPhysicalKeyPressed(Key.Shift) && inputDir.Y < 0)
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
    private void Animate()
    {
        /*
         * Sets the player's animation based on whether the player is
         * moving and if they are on the floor.
         */

        if (Velocity != Vector3.Zero && IsOnFloor())
            newAnim = ANIM.MOVING;
        else if (Velocity != Vector3.Zero && !IsOnFloor())
            newAnim = ANIM.MOVINGANDJUMPING;
        else if (Velocity == Vector3.Zero && !IsOnFloor())
            newAnim = ANIM.JUMPING;
        else
            newAnim = ANIM.IDLE;

        if (newAnim != previousAnim)
        {
            switch (newAnim)
            {
                case ANIM.MOVING:
                    animTree.Set(walkBlend, 1);
                    dupeBodyAnimTree.Set(walkBlend, 1);
                    animTree.Set(jumpRequest, (int)AnimationNodeOneShot.OneShotRequest.Abort);
                    dupeBodyAnimTree.Set(jumpRequest, (int)AnimationNodeOneShot.OneShotRequest.Abort);
                    break;
                case ANIM.JUMPING:
                    animTree.Set(jumpRequest, (int)AnimationNodeOneShot.OneShotRequest.Fire);
                    dupeBodyAnimTree.Set(jumpRequest, (int)AnimationNodeOneShot.OneShotRequest.Fire);
                    break;
                case ANIM.MOVINGANDJUMPING:
                    animTree.Set(walkBlend, 1);
                    dupeBodyAnimTree.Set(walkBlend, 1);
                    // clang-format off
                    goto case ANIM.JUMPING;
                case ANIM.IDLE:
                    // clang-format on
                    animTree.Set(walkBlend, 0);
                    dupeBodyAnimTree.Set(walkBlend, 0);
                    animTree.Set(jumpRequest, (int)AnimationNodeOneShot.OneShotRequest.Abort);
                    dupeBodyAnimTree.Set(jumpRequest, (int)AnimationNodeOneShot.OneShotRequest.Abort);
                    break;
            }
        }
        previousAnim = newAnim;
    }
    private async void Attack()
    {
        // Plays the attack animation and waits for the AnimationFinished signal to fire.
        // The animation state machine detects the attacking bool and plays the animation in the state machine.
        attacking = true;
        dupeBodyAnimTree.Set("parameters/Attack/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
        attackCooldown.Start();
        await ToSignal(attackCooldown, Timer.SignalName.Timeout);
    }
    private void OnAnimationFinished(StringName name)
    {
        if (name == "custom/blockShove")
        {
            blockShove = false;
            if (attackRange.HasOverlappingBodies())
            {
                Godot.Collections.Array<Node3D> gladiatorsHit = attackRange.GetOverlappingBodies();
                for (int counter = 0; counter < gladiatorsHit.Count; counter++)
                {
                    gladiatorsHit[counter].EmitSignal(Gladiator.SignalName.Stagger);
                }
            }
        }
    }
    private void OnTimerTimeout()
    {
        attacking = false;
        if (attackRange.HasOverlappingBodies())
        {
            Godot.Collections.Array<Node3D> gladiatorsHit = attackRange.GetOverlappingBodies();
            Vector3[] gladiatorsPos = new Vector3[gladiatorsHit.Count];
            Node3D gladiatorHit;
            int targetsHit = gladiatorsHit.Count;
            for (int counter = 0; counter < targetsHit; counter++)
            {
                gladiatorsPos[counter] = gladiatorsHit[counter].Position;
            }
            gladiatorHit = gladiatorsHit[0];
            if (targetsHit > 1)
            {
                for (int counter = 1; counter < targetsHit; counter++)
                {
                    if ((gladiatorsPos[counter] - Position) < (gladiatorsPos[counter - 1] - Position))
                        gladiatorHit = gladiatorsHit[counter];
                }
            }
            gladiatorHit.EmitSignal(Gladiator.SignalName.Hit, 10);
            gladiatorHit.EmitSignal(Gladiator.SignalName.Stagger);
        }
    }
    private void OnHit(int damage)
    {
        if (!dead)
        {
            health -= damage;
            if (health <= 0)
            {
                dead = true;
            }
        }
    }
}
