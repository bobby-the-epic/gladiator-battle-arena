using Godot;
using System;

public partial class Player : CharacterBody3D
{
    const float speed = 8.0f;
    const float sprintSpeed = 12.0f;
    const float jumpVelocity = 4.5f;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    int health = 100;
    bool dead = false;

    const int maxHealth = 100;
    const int fire = (int)AnimationNodeOneShot.OneShotRequest.Fire;
    const int abort = (int)AnimationNodeOneShot.OneShotRequest.Abort;
    int weaponDamage = 20;
    float rotationInput;
    float tiltInput;
    float pitch;
    float tiltLowerLimit = Mathf.DegToRad(-85);
    float tiltUpperLimit = Mathf.DegToRad(85);
    Vector3 mouseRotation;
    AudioStreamPlaybackPolyphonic playerAudioStream;
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

    /*  I used a duplicate body mesh for the shadows. I wanted the camera to be
     able to see the player's body, but I also had to do first person
     animations. I separated the hands from the mesh with a BoneAttachment3D
     node and animated that. */

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
    [Export]
    RayCast3D rayCast;
    [Export]
    Timer attackCooldown;
    [Export]
    AnimationTree animTree, dupeBodyAnimTree;
    [Export]
    Control hud;
    [ExportGroup("Audio")]
    [Export]
    AudioStreamPlayer playerAudio;
    [Export]
    AudioStream swordSwingSfx, swordHitSfx, playerHitSfx;

    [Signal]
    public delegate void HitEventHandler(int damage, CharacterBody3D gladiator);

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        playerAudioStream = (AudioStreamPlaybackPolyphonic)playerAudio.GetStreamPlayback();

        attackCooldown.Timeout += () => attacking = false;
        animTree.AnimationFinished += OnAnimationFinished;
        Hit += OnHit;
    }
    public override void _ExitTree()
    {
        Hit -= OnHit;
    }
    public override void _PhysicsProcess(double delta)
    {
        SetMovement(delta);
        Animate();
        UpdateCamera(delta);
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseEvent &&
            Input.MouseMode == Input.MouseModeEnum.Captured)
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
        cameraController.Rotation =
            new Vector3(pitch, cameraController.Rotation.Y, cameraController.Rotation.Z);

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
        if (Input.IsActionJustPressed("attack") && !attacking && !blocking)
            Attack();
        // The animTree detects the blocking bool and plays the animations in its state machine.
        if (Input.IsActionJustPressed("block"))
            blocking = true;
        if (Input.IsActionJustReleased("block"))
            blocking = false;
        if (blocking && Input.IsActionJustPressed("attack"))
            blockShove = true;

        /*  Sets the player's animation based on whether the player is
         moving and if they are on the floor. */

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
                    animTree.Set(jumpRequest, abort);
                    dupeBodyAnimTree.Set(jumpRequest, abort);
                    break;
                case ANIM.JUMPING:
                    animTree.Set(jumpRequest, fire);
                    dupeBodyAnimTree.Set(jumpRequest, fire);
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
                    animTree.Set(jumpRequest, abort);
                    dupeBodyAnimTree.Set(jumpRequest, abort);
                    break;
            }
        }
        previousAnim = newAnim;
    }
    private async void Attack()
    {
        attacking = true;

        if (rayCast.IsColliding())
        {
            CharacterBody3D target = (CharacterBody3D)rayCast.GetCollider();
            target.EmitSignal(Gladiator.SignalName.Hit, weaponDamage);
            playerAudioStream.PlayStream(swordHitSfx, volumeDb: Main.volume);
        }
        else
            playerAudioStream.PlayStream(swordSwingSfx, volumeDb: Main.volume + 10);

        /* Plays the attack animation and waits for the AnimationFinished signal to fire.
        The animation state machine detects the attacking bool and plays the animation in the
        state machine. */

        dupeBodyAnimTree.Set("parameters/Attack/request", fire);
        attackCooldown.Start();
        await ToSignal(attackCooldown, Timer.SignalName.Timeout);
    }
    private void OnAnimationFinished(StringName name)
    {
        if (name == "custom/blockShove")
        {
            blockShove = false;
            if (rayCast.IsColliding())
            {
                CharacterBody3D target = (CharacterBody3D)rayCast.GetCollider();
                target.EmitSignal(Gladiator.SignalName.Staggered);
            }
            playerAudioStream.PlayStream(swordSwingSfx, volumeDb: Main.volume + 10);
        }
    }
    private void OnHit(int damage, CharacterBody3D gladiator)
    {
        Vector3 gladiatorDirection = Transform.Origin.DirectionTo(gladiator.Transform.Origin);
        Vector3 forwardVector = -Transform.Basis.Z.Normalized();
        float angle = -forwardVector.SignedAngleTo(gladiatorDirection, Vector3.Up);

        if (!dead)
        {
            if (blocking)
            {
                if ((bool)gladiator.Get(Gladiator.PropertyName.onScreen) == true)
                    return;
            }

            playerAudioStream.PlayStream(playerHitSfx, volumeDb: Main.volume);

            health -= damage;
            if (health <= 0)
            {
                dead = true;
                SignalBus.Instance.EmitSignal(SignalBus.SignalName.PlayerDied);
                return;
            }
            SignalBus.Instance.EmitSignal(SignalBus.SignalName.DamageTaken, health, angle);
        }
    }
}
