using NUnit.Framework;
using UnityEngine;
public class BurnDamagestrategy : ISkillStrategy
{
    public BurnDamageConfig _config;
    public BowScript _bow;

    public BurnDamagestrategy(BurnDamageConfig config, BowScript bow)
    {
        _config = config;
        _bow = bow;
    }

    public void Activate()
    {
        _bow.isBurning = true;
        _bow.burningDamage = _config.burnDamage;
        _bow.burningDuration = _config.skillDuration;
    }

    public void Deactivate()
    {
        _bow.isBurning = false;
    }

    public void Apply()
    {
        
    }
}
