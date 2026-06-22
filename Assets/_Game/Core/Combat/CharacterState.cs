public class CharacterState 
{
    public float maxHealth; 
    public float currentHealth;
    public bool TakeDamage(float damage) 
    { 
        currentHealth -= damage; 
        if (currentHealth <= 0) 
        { 
            return true;
        } 
        return false;
    }
}