using UnityEngine;

public class RageModeStrategy : ISkillStrategy
{
    public RageModeConfig _config;
    public SkillManager _manager;
    public StatSet stats;

    public RageModeStrategy(RageModeConfig config, SkillManager manager, StatSet stats)
    {
        _config = config;
        _manager = manager;
        this.stats = stats;
    }

    public void Activate()
    {
        stats.Add(new Modifier("Damage", ModType.More, _config.rageMultipler, this));
    }

    public void Deactivate()
    {
        stats.RemoveBySource(this);
    }

    public void Apply()
    {
        
    }
}
