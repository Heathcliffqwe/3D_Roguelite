using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    public CharacterState state;
    public HealthBar healthBar;

    void Start()
    {
        state = new CharacterState { maxHealth = 100, currentHealth = 100 };
    }
    
    public void TakeDamage(int damage)
    {
        if(state.TakeDamage(damage))
            Destroy(gameObject);
        healthBar.UpdateHealth((float)state.currentHealth/(float)state.maxHealth);
    }
}
