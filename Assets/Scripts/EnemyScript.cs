using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using Object = System.Object;

public class EnemyScript : MonoBehaviour
{
    public PlayerScript Player;
    public int maxHealth;
    public HealthBar healthBar;
    public float moveSpeed = 5f;
    public float attackRange = 3f;
    public int attackDamage;
    public float attackCooldown;
    private bool isAttacking;
    private bool isMoveing;
    private GameObject _player;
    private Animator _animator;
    public string enemyName;
    
    private int curHealth;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _player = GameObject.FindGameObjectWithTag("Player");
        curHealth = maxHealth;
    }
    

    public void TakeDamage(int damage)
    {
        if(curHealth <= 0){return;}
        curHealth -= damage;
        Debug.Log("TakeDamage çalıştı: " + enemyName);
        EnemyHbUI.Instance.ShowEnemy(enemyName, (float)curHealth/(float)maxHealth);
        if (curHealth <= 0)
        {
            EnemyHbUI.Instance.Hide();
            Destroy(gameObject);
        }
    }

    public void ApplyBurn(float duration, float damagePerTick) //
    {
        StartCoroutine(BurnCoroutine(duration, damagePerTick));
    }
    
    IEnumerator BurnCoroutine(float duration, float damagePerTick) // Yapay
    {
        float elapsed = 0f;                                         // Zeka
    
        while (elapsed < duration)
        {
            if (curHealth <= 0) {break;}                            // Tarafından
            TakeDamage((int)damagePerTick);
            yield return new WaitForSeconds(1f);                  // Yazılmıştır
            elapsed += 1f;
        }
    }                                                          //
    
    void FixedUpdate()
    {
        if (_player == null) return;
        float mesafe = Vector3.Distance(transform.position, _player.transform.position);
        var direction = (_player.transform.position - transform.position).normalized;
        direction.y = 0;
        Quaternion dir = Quaternion.LookRotation(direction);
        isMoveing = true;
        transform.rotation = Quaternion.Slerp(transform.rotation, dir, 10f * Time.deltaTime);
        transform.position += direction * Time.deltaTime * moveSpeed;
        
        if( mesafe < attackRange)
        {
            _animator.SetBool("isattack", true);
        }
        if( mesafe > attackRange)
        {
            _animator.SetBool("isattack", false);
        }
    }

    

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isMoveing = false;
            isAttacking = true;
            Debug.Log("true");
            other.GetComponent<PlayerScript>().TakeDamageFromEnemys((int)attackDamage);
            StartCoroutine(WaitCoroutine(attackCooldown));
        }
    }

    IEnumerator WaitCoroutine(float cooldown)
    {
        Debug.Log("nfalse");
        Debug.Log("false");
        isAttacking = false;
        
        yield return new WaitForSeconds(attackCooldown);
    }


    // private GameObject _target;
    // private void FindTarget()
    // {
    //     if(!_player)
    //         _player = GameObject.FindGameObjectWithTag("Player");
    //
    //     float distence = Vector3.Distance(transform.position, _player.transform.position);
    //     if (distence > attackRange)
    //     {
    //         _target = this.gameObject;
    //     }
    //     else
    //     {
    //         _target = null;
    //     }
    // }
    //
    // private float _tick;
    // private static float TickLimit = 10f;
    //
    // private GameObject _player;
    //
    // void Update()
    // {
    //     _tick += Time.deltaTime;
    //
    //     if (_tick > TickLimit)
    //     {
    //         FindTarget();
    //
    //         _tick = 0;
    //     }
    //
    //     if (_target)
    //     {
    //         var trTarget = _target.transform;
    //         
    //         if(!_player)
    //             _player = GameObject.FindGameObjectWithTag("Player");
    //         
    //         _target.transform.LookAt(_player.transform);
    //         
    //         
    //         Vector3 yon = _player.transform.position - transform.position;
    //
    //         var pos = _target.transform.position;
    //
    //         pos += yon * moveSpeed * Time.deltaTime;
    //
    //         _target.transform.position = pos;
    //
    //     }
    //     
    // }
}
