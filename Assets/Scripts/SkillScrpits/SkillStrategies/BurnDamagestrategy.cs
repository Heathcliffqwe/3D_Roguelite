using NUnit.Framework;
using UnityEngine;
public class BurnDamagestrategy : ISkillStrategy
{
    public BurnDamageConfig _config;
    public StatSet statSet;

    public BurnDamagestrategy(BurnDamageConfig config, StatSet stats)
    {
        _config = config;

        this.statSet = stats;
    }

    public void Activate()
    {
        statSet.Add(new Modifier("BurningDamage", ModType.Increased, _config.burnDamage, this));
        statSet.Add(new Modifier("BurningDuration", ModType.Added, _config.skillDuration, this));
    }

    public void Deactivate()
    {
        statSet.RemoveBySource(this);
    }

    public void Apply()
    {
        
    }
}
