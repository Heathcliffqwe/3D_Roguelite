using System;
using UnityEngine;

public class BowScript : MonoBehaviour
{
    public float speed;
    public float damage;
    public bool isBurning;
    public float burningDuration;
    public float burningDamage; 
    void Start()
    {
        Destroy(gameObject, 5f);
    }
    
    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<EnemyScript>().TakeDamage((int)damage);
            speed = 0f;
            if (isBurning)
            {
                other.GetComponent<EnemyScript>().ApplyBurn(burningDuration,burningDamage);
            }
            transform.SetParent(other.transform);
            Destroy(gameObject, 1f);
            
        }
    }
}
