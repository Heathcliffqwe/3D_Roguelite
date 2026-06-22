using UnityEngine;
public class AttackSpeedStrategy : ISkillStrategy
{
    public AttackSpeedConfig _config;
    public AutoAttackScript _auto;
    public StatSet stats;

    public AttackSpeedStrategy(AttackSpeedConfig config, AutoAttackScript auto,  StatSet stats)
    {
        _config = config;
        _auto = auto;
        this.stats = stats;
        
    }

    public void Activate()
    {
        stats.Add(new Modifier("AttackSpeed", ModType.Increased, _config.speedMultipler, this));
    }

    public void Deactivate()
    {
        stats.RemoveBySource(this);
    }

    public void Apply()
    {
        
    }
}
