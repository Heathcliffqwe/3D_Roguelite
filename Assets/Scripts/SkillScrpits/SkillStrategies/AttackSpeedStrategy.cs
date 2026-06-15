using UnityEngine;
public class AttackSpeedStrategy : ISkillStrategy
{
    public AttackSpeedConfig _config;
    public AutoAttackScript _auto;

    public AttackSpeedStrategy(AttackSpeedConfig config, AutoAttackScript auto)
    {
        _config = config;
        _auto = auto;
    }

    public void Activate()
    {
        _auto.attackCooldown /= _config.speedMultipler;
    }

    public void Deactivate()
    {
        _auto.attackCooldown *= _config.speedMultipler;
    }

    public void Apply()
    {
        
    }
}
