using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractEffectDecorator : IEffectDecorator
{
    // eg. new EncryptionDecorator(source), EncryptionDecorator's wrappee is source
    protected IEffectDecorator effectWrappee;

    public AbstractEffectDecorator(IEffectDecorator effectWrappee)
    {
        this.effectWrappee = effectWrappee;
    }

    public void setEffectWrappee(IEffectDecorator effectWrappee)
    {
        this.effectWrappee = effectWrappee;
    }

    public IEffectDecorator getEffectWrappee()
    {
        return effectWrappee;
    }

    public abstract EnvironmentalHazardsManager.EnvironmentalHazardType getHazardType();

    public abstract int DamageEffect();
    public abstract float MovementSpeedEffect();

    
}
