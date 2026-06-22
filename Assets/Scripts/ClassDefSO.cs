using UnityEngine;

[CreateAssetMenu(fileName = "ClassDefSO", menuName = "Scriptable Objects/ClassDefSO")]
public class ClassDefSO : ScriptableObject
{
    public string className;
    public float baseHealth;
    public float baseMana;
    public int baseDamage;
    public float baseAttackCooldown;
    public float baseMoveSpeed;
    public WeponType weponType;
    public int baseStrength;
    public int baseIntelligence;
    public int baseDexterity;
    public int baseWisdom;
    
    public ClassDef ToClassDef()
    {
        return new ClassDef(className,baseHealth,baseMana,baseDamage,baseAttackCooldown,
            baseMoveSpeed,weponType,baseStrength,baseIntelligence,baseDexterity,baseWisdom);
    }
}
