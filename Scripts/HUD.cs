using Godot;
using System;

public partial class HUD : Control
{
    float initialHealthBarSize;
    TextureRect damageEffect;
    ColorRect healthBar;

    [Signal]
    public delegate void DamageTakenEventHandler(float damage, float health, float maxHealth);

    public override void _Ready()
    {
        damageEffect = GetNode<TextureRect>("TextureRect");
        healthBar = GetNode<ColorRect>("HealthBar/ColorRect");
        initialHealthBarSize = healthBar.Size.X;

        DamageTaken += OnDamageTaken;
    }
    private void OnDamageTaken(float damage, float health, float maxHealth)
    {
        // Resizes the health bar when the player takes damage.
        Vector2 healthCalc = new Vector2();
        healthCalc = new Vector2((((float)health / maxHealth) * initialHealthBarSize), healthBar.Size.Y);
        healthBar.SetSize(healthCalc);
    }
}
