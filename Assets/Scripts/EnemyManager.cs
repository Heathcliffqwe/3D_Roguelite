using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    private float maxX = 15f;
    private float minX = -15f;
    private float maxZ = 15f;
    private float minZ = -15f;
    public GameObject enemy;
    private int maxEnemies = 5;
    void Start()
    {
        for (int i = 0; i < maxEnemies; i++)
        {
            Vector3 random= new Vector3(Random.Range(minX, maxX), 0.5f, Random.Range(minZ, maxZ));
            Instantiate(enemy, random, Quaternion.identity);
        }
    }
    
    void Update()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length < maxEnemies)
        {
            Vector3 random= new Vector3(Random.Range(minX, maxX), 0.5f, Random.Range(minZ, maxZ));
            Instantiate(enemy, random, Quaternion.identity);
        }

        if (enemies.Length > maxEnemies)
        {
            StopAllCoroutines();
        }
        
    }
}
