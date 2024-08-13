using Godot;
using System;

public partial class Humanoid : CharacterBody3D
{
    protected bool attacking = false;
    [Export]
    protected AnimationPlayer animationPlayer;

    public override void _Ready()
    {
        animationPlayer.AnimationFinished += OnAnimationFinished;
    }
    protected async void Attack()
    {
        attacking = true;
        animationPlayer.Play("swordSwing");
        await ToSignal(animationPlayer, AnimationPlayer.SignalName.AnimationFinished);
    }
    private void OnAnimationFinished(StringName animName)
    {
        if (animName == "swordSwing")
            attacking = false;
    }

}
