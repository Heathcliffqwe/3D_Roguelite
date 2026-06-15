using UnityEngine;

public class RageModeStrategy : ISkillStrategy
{
    public RageModeConfig _config;
    public SkillManager _manager;

    public RageModeStrategy(RageModeConfig config, SkillManager manager)
    {
        _config = config;
        _manager = manager;
    }

    public void Activate()
    {
        foreach (var skill in _manager.activeSkills)
        {
            if (skill.Key.Equals(_config))
            {
                continue;
            }
            else
            {
                skill.Value.Deactivate();
                if (skill.Key is MultiShotConfig multi)
                {
                    multi.extraArrow *= (int)_config.rageMultipler;
                }

                if (skill.Key is BurnDamageConfig burn)
                {
                    burn.skillDuration *= (int)_config.rageMultipler;
                }

                if (skill.Key is AttackSpeedConfig speed)
                {
                    speed.speedMultipler *= (int)_config.rageMultipler;
                }
                skill.Value.Activate();
            }
        }
    }

    public void Deactivate()
    {
        foreach (var skill in _manager.activeSkills)
        {
            if (skill.Key.Equals(_config))
            {
                continue;
            }
            else
            {
                skill.Value.Deactivate();
                if (skill.Key is MultiShotConfig multi)
                {
                    multi.extraArrow /= (int)_config.rageMultipler;
                }

                if (skill.Key is BurnDamageConfig burn)
                {
                    burn.skillDuration /= (int)_config.rageMultipler;
                }

                if (skill.Key is AttackSpeedConfig speed)
                {
                    speed.speedMultipler /= (int)_config.rageMultipler;
                }
                skill.Value.Activate();
            }
        }
    }

    public void Apply()
    {
        
    }
}
