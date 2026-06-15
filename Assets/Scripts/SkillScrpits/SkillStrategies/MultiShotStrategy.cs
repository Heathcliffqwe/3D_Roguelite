using UnityEngine;

public class MultiShotStrategy : ISkillStrategy
{
    public MultiShotConfig _config;
    public AutoAttackScript _auto;

    public MultiShotStrategy(MultiShotConfig config, AutoAttackScript auto)
    {
        _config = config;
        _auto = auto;
    }

    public void Activate()
    {
        _auto.arrowCount += _config.extraArrow;
    }

    public void Deactivate()
    {
        _auto.arrowCount -= _config.extraArrow;
    }

    public void Apply()
    {
        
    }
}
