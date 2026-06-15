using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    public int maxHealth;
    public HealthBar healthBar;
    
    private int curHealth;
    void Start()
    {
        curHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        curHealth -= damage;
        healthBar.UpdateHealth((float)curHealth/(float)maxHealth);
        if (curHealth <= 0)
        {
            Destroy(gameObject);
        }
    }
}
