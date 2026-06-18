using Unity.VisualScripting;
using UnityEngine;

public class Moderator : MonoBehaviour
{
    public Transform bossSpawn;
    public GameObject bossenemy;
    public GameObject player;
    public Transform playerSpawn;

    void Start()
    {
        Instantiate(bossenemy, bossSpawn.position, Quaternion.identity);
        Instantiate(player, playerSpawn.position, Quaternion.identity);
    }
}