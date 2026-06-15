using UnityEngine;

public class AutoAttackScript : MonoBehaviour
{
    public GameObject arrow;
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

            if (timer >= attackCooldown)
            {
                Instantiate(arrow, firePoint.position, transform.rotation); //
                timer = 0;
            }
        }
    }
}
