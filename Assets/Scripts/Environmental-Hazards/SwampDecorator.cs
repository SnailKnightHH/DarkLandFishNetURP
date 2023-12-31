using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwampDecorator : AbstractEffectDecorator
{
    private float SwarmMovementSpeedDebuff = 3f;

    public SwampDecorator(IEffectDecorator effectWrappee) : base(effectWrappee)
    {

    }

    public override int DamageEffect()
    {
        return effectWrappee.DamageEffect();
    }

    public override float MovementSpeedEffect()
    {
        return effectWrappee.MovementSpeedEffect() - SwarmMovementSpeedDebuff;
    }

    public override EnvironmentalHazardsManager.EnvironmentalHazardType getHazardType()
    {
        return EnvironmentalHazardsManager.EnvironmentalHazardType.Swamp;
    }

}
