using System;
using UnityEngine;

public class VfxManager : MonoBehaviour
{
    public SkillManager manager;
    public ParticleSystem a1;
    public ParticleSystem a2;
    public RageModeConfig rage;
    public ParticleSystem flame;
    public BurnDamageConfig burnDamage;
    void Start()
    {
        flame = GetComponentInChildren<ParticleSystem>(true);
        flame.Stop();
        a1.Stop();
        a2.Stop();
        manager.OnSkillActivated += OnSkillChanged;
        manager.OnSkillDeactivated += OnSkillChanged;
    }
    void OnSkillChanged(SkillConfig skill)
    {
        if (skill == burnDamage)
        {
            if (manager.IsSkillActive(burnDamage))
            {
                flame.Play();
            }
            else
            {
                flame.Stop();
            }
        }

        if (skill == rage)
        {
            if (manager.IsSkillActive(rage))
            { a1.Play(); a2.Play(); }
            else
            { a1.Stop(); a2.Stop(); }
        }
    }
    void OnDestroy()
    {
        manager.OnSkillActivated -= OnSkillChanged;
        manager.OnSkillDeactivated -= OnSkillChanged;
    }
}