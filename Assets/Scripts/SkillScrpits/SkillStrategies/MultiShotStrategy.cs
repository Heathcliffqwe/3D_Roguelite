using UnityEngine;

public class MultiShotStrategy : ISkillStrategy
{
    public MultiShotConfig _config;
    public AutoAttackScript _auto;
    public StatSet statSet;

    public MultiShotStrategy(MultiShotConfig config, AutoAttackScript auto, StatSet stats)
    {
        _config = config;
        _auto = auto;
        this.statSet = stats;
    }

    public void Activate()
    {
        statSet.Add(new Modifier("ArrowCount", ModType.Added, _config.extraArrow, this));
    }

    public void Deactivate()
    {
        statSet.RemoveBySource(this);
    }

    public void Apply()
    {
        
    }
}
