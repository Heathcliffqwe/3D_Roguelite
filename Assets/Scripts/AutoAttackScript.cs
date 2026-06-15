using UnityEngine;

public class AutoAttackScript : MonoBehaviour
{
    public GameObject arrow;
    public int arrowCount = 1;
    public Transform firePoint;
    public float attackCooldown;
    public float attackRange;
    private float timer;
    public PlayerMovementScript player;
    void Start()
    {
        
    }

 
    void Update()
    {
        timer += Time.deltaTime;
        if (player.isMoving)
        {
            timer = 0;
            return;
        }else
        {
            var enemies  = GameObject.FindGameObjectsWithTag("Enemy");
            float enYakınMesafe = Mathf.Infinity; //
            GameObject enYakınDusman = null;
            foreach (var enemy in enemies)
            {
                float mesafe = Vector3.Distance(transform.position, enemy.transform.position);
                if (mesafe < enYakınMesafe)
                {
                    enYakınMesafe = mesafe;
                    enYakınDusman = enemy;
                }
            }
            if (enYakınDusman == null) return;
            Vector3 yon = enYakınDusman.transform.position - transform.position;
            yon.y = 0;
            Quaternion hedef = Quaternion.LookRotation(yon);
            transform.rotation = Quaternion.Slerp(transform.rotation, hedef, 10f * Time.deltaTime);
            float aciFarki = Quaternion.Angle(transform.rotation, hedef);
            if(aciFarki > 5f)
                return;
            for (int i = 0; i < arrowCount; i++)
            {
                if (timer >= attackCooldown)
                {
                    for (int j = 0; j < arrowCount; j++)
                    {
                        float açıOffset = (j - (arrowCount - 1) / 2f) * 10f;
                        Quaternion okRotasyonu = transform.rotation * Quaternion.Euler(0, açıOffset, 0);
                        Instantiate(arrow, firePoint.position, okRotasyonu);
                    }
                    timer = 0;
                }
            }
        }
    }
}
