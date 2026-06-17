using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public Transform[] spawnPoints;
    public Transform bossSpawn;
    private int maxEnemies = 5;
    public GameObject[] defaultenemies;
    public GameObject bossenemy;
    public float bossSpawnTime = 5f;
    private float elapsedTime;
    public bool isBossSpawned;
    void Start()
    {
        elapsedTime = 0;
        for (int i = 0; i < maxEnemies; i++)
        {
            Vector3 spawnpos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
            Instantiate(defaultenemies[Random.Range(0, defaultenemies.Length)], spawnpos, Quaternion.identity);
        }
    }
    
    void Update()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime < bossSpawnTime)
        {
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemies.Length < maxEnemies)
            {
                Vector3 spawnpos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
                Instantiate(defaultenemies[Random.Range(0, defaultenemies.Length)], spawnpos, Quaternion.identity);
            }

            if (enemies.Length > maxEnemies)
            {
                StopAllCoroutines();
            }
        }

        if (elapsedTime >= bossSpawnTime && !isBossSpawned)
        {
            Instantiate(bossenemy, bossSpawn.position, Quaternion.identity);
            elapsedTime = 0;
            isBossSpawned = true;
        }

    }
}
