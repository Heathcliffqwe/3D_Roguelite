public class EnemyState 
{
    public int maxHealth;
    public int curHealth;
    public float moveSpeed = 5f;
    public float attackRange = 2f;
    public int attackDamage;
    public float attackCooldown;
    public string enemyName;
    private float attackTimer;
    public bool isburning;
    public float burnTimeRemaining;
    public float burnTickTimer;
    public int burnDamagePerTick;
    
    public bool TakeDamage(int damage)
    {
        if(curHealth <= 0)
        {return false;}
        curHealth -= damage;
        return true;
    }

    public void ApplyBurn(float duration, int damagePerTick)
    {
        isburning = true;
        burnTimeRemaining = duration;
        burnDamagePerTick = damagePerTick;
    }
    public void BurnTick(float deltaTime)
    {
        if (isburning)
        {
            if ((burnTimeRemaining -= deltaTime) < 0)
            {
                isburning = false;
                return;
            }

            if ((burnTickTimer += deltaTime) > 1)
            {
                TakeDamage(burnDamagePerTick);
                burnTickTimer = 0;
            }
        }
    }

    public bool TryAttack(float distance, float deltaTime)
    {
        attackTimer -= deltaTime;
        if (distance < attackRange && attackTimer< 0)
        { 
            attackTimer = attackCooldown;
            return true;
        }
        return false;
    }
}