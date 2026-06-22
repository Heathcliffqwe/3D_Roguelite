using UnityEngine;

public class AutoAttackScript : MonoBehaviour
{
    public GameObject arrow;
    public Transform firePoint;
    public float baseAttackCooldown;
    public float basedamage;
    private float timer;
    public PlayerMovementScript player;
    public StatSet stats;
    public SkillManager skillManager;
    public BurnDamageConfig burnConfig;
 
    void Update()
    {
        float arrowCount = stats.Get("ArrowCount", 1);
        float attackspeed=  stats.Get("AttackSpeed", baseAttackCooldown);
        timer += Time.deltaTime;
        if (player.isMoving)
        {
            timer = 0;
            return;
        }else
        {
            var enemies  = GameObject.FindGameObjectsWithTag("Enemy");
            float enYakınMesafe = Mathf.Infinity; //
            GameObject enYakinDusman = null;
            foreach (var enemy in enemies)
            {
                float mesafe = Vector3.Distance(transform.position, enemy.transform.position);
                if (mesafe < enYakınMesafe)
                {
                    enYakınMesafe = mesafe;
                    enYakinDusman = enemy;
                }
            }
            if (enYakinDusman == null) return;
            Vector3 yon = enYakinDusman.transform.position - transform.position;
            yon.y = 0;
            Quaternion hedef = Quaternion.LookRotation(yon);
            transform.rotation = Quaternion.Slerp(transform.rotation, hedef, 10f * Time.deltaTime);
            float aciFarki = Quaternion.Angle(transform.rotation, hedef);
            if(aciFarki > 5f)
                return;
            if (timer >= attackspeed)
            {
                for (int j = 0; j < arrowCount; j++)
                {
                    float aciOffset = (j - (arrowCount - 1) / 2f) * 10f;
                    Quaternion okRotasyonu = transform.rotation * Quaternion.Euler(0, aciOffset, 0);
                    var ok = Instantiate(arrow, firePoint.position, okRotasyonu);
                    var bow = ok.GetComponent<BowScript>();
                    bow.stats = stats;
                    bow.damage = stats.Get("Damage", basedamage);
                    bow.isBurning = skillManager.IsSkillActive(burnConfig);
                }

                timer = 0;
            }

        }
    }
}
