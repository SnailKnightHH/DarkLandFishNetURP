
public class PlayerBaseEffect : IEffectDecorator
{
    private float baseMovementSpeed;
    private int baseDamage;

    public PlayerBaseEffect(float baseMovementSpeed, int baseDamage)
    {
        this.baseMovementSpeed = baseMovementSpeed;
        this.baseDamage = baseDamage;
    }

    public int DamageEffect()
    {
        return baseDamage;
    }

    public float MovementSpeedEffect()
    {
        return baseMovementSpeed;
    }


}
