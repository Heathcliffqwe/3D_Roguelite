public enum WeponType
{
    Axe,
    Sword,
    Bow,
    Spear,
    Wand,
    LongSword
}
public record ClassDef(string ClassName,float BaseHealth,float BaseMana,
    int BaseDamage,float BaseAttackCooldown,float BaseMoveSpeed,WeponType WeponType,
    int BaseStrength,int BaseIntelligence,int BaseDexterity, int BaseWisdom);

