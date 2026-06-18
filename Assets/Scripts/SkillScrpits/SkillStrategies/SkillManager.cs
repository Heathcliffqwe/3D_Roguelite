using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour 
{
    public AutoAttackScript autoAttack;
    public BowScript bow;
    public Dictionary<SkillConfig, ISkillStrategy> activeSkills = new Dictionary<SkillConfig, ISkillStrategy>();
    public event System.Action<SkillConfig> OnSkillActivated;
    public event System.Action<SkillConfig> OnSkillDeactivated;
    
    public ISkillStrategy CreateStrategy(SkillConfig config)
    {
        if (config is MultiShotConfig multiShot)
            return new MultiShotStrategy(multiShot, autoAttack);
    
        if (config is BurnDamageConfig burn)
            return new BurnDamagestrategy(burn, bow);
    
        if (config is AttackSpeedConfig speed)
            return new AttackSpeedStrategy(speed, autoAttack);

        if (config is RageModeConfig rage)
            return new RageModeStrategy(rage, this);
    
        return null;
    }

    public void ToggleSkill(SkillConfig config) //
    {
        if (activeSkills.ContainsKey(config))
        {
            activeSkills[config].Deactivate();
            activeSkills.Remove(config);
            OnSkillDeactivated?.Invoke(config);
        }
        else
        {
            ISkillStrategy strategy = CreateStrategy(config);
            strategy.Activate();
            activeSkills.Add(config, strategy);
            OnSkillActivated?.Invoke(config);
        }
    }

    public bool IsSkillActive(SkillConfig config)
    {
        if (activeSkills.ContainsKey(config))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
