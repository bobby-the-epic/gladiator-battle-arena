using Godot;
using System;

public partial class HUD : Control
{
    struct Circle
    {
        public Vector2 center, radius;
    }

    TextureRect damageDirection, damageAlert;
    ProgressBar healthBar;
    Circle circle;
    AnimationTree animTree;
    StringName damageTransition = new StringName("parameters/Transition/transition_request");

    public override void _Ready()
    {
        damageDirection = GetNode<TextureRect>("DamageDirection");
        damageAlert = GetNode<TextureRect>("DamageAlert");
        healthBar = GetNode<ProgressBar>("HealthBar/ProgressBar");
        animTree = GetNode<AnimationTree>("AnimationTree");

        SignalBus.Instance.DamageTaken += OnDamageTaken;
        CircleInit();
    }
    private void CircleInit()
    {
        circle.radius.X = Size.X / 2 - 100;
        circle.radius.Y = Size.Y / 2 - 100;
        circle.center = new Vector2(Size.X / 2, Size.Y / 2);
    }
    private void OnDamageTaken(int health, float angle)
    {
        // Resizes the health bar when the player takes damage.
        healthBar.SetValueNoSignal(health);
        DamageAlert(angle);
    }
    private void DamageAlert(float angle)
    {
        // Rotates the texture in the direction of where the enemy hit the player from.
        damageDirection.Rotation = angle;
        // Rotate the angle such that 0 degrees points up.
        angle -= Mathf.Pi / 2;

        // Makes the angle positive (only if it is negative) by adding a full rotation.
        if (angle < 0)
            angle += Mathf.Tau;

        // Positions the texture on the screen on a point of a circle,
        // which is defined by the size of the screen minus an offset.
        damageDirection.Position = new Vector2(
            (circle.center.X + circle.radius.X * Mathf.Cos(angle)) - damageDirection.PivotOffset.X,
            (circle.center.Y + circle.radius.Y * Mathf.Sin(angle)) - damageDirection.PivotOffset.Y);
        animTree.Set(damageTransition, "damaged");
    }
}
