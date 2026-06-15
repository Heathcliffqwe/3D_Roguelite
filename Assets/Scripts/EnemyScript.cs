using System.Collections;
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
        if(curHealth <= 0){return;}
        curHealth -= damage;
        healthBar.UpdateHealth((float)curHealth/(float)maxHealth);
        if (curHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void ApplyBurn(float duration, float damagePerTick)
    {
        StartCoroutine(BurnCoroutine(duration, damagePerTick));
    }
    
    IEnumerator BurnCoroutine(float duration, float damagePerTick)
    {
        float elapsed = 0f;
    
        while (elapsed < duration)
        {
            if (curHealth <= 0) {break;}
            TakeDamage((int)damagePerTick);
            yield return new WaitForSeconds(0.5f);  // 
            elapsed += 0.5f;
        }
    }
}
