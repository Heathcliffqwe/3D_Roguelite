using UnityEngine;
using UnityEngine.AI;

public class EnemyScript : MonoBehaviour
{
    public HealthBar healthBar;
    private GameObject _player;
    private Animator _animator;
    public string enemyName;
    public NavMeshAgent agent;
    public EnemyState state;
    

    void Start()
    {
        _animator = GetComponent<Animator>();
        _player = GameObject.FindGameObjectWithTag("Player");
        agent = GetComponent<NavMeshAgent>();
        state = new EnemyState{curHealth = 100, maxHealth = 100};
        agent.speed = state.moveSpeed;
        
    }
    

    public void TakeDamage(int damage)
    {
        state.TakeDamage(damage);
        EnemyHbUI.Instance.ShowEnemy(enemyName, (float)state.curHealth/(float)state.maxHealth);
        if (state.curHealth <= 0)
        {
            EnemyHbUI.Instance.Hide();
            Destroy(gameObject);
        }
    }

    public void ApplyBurn(float duration, int damagePerTick) 
    {
        state.ApplyBurn(duration, damagePerTick);
    }
    
    void Update()
    {
        state.BurnTick(Time.deltaTime);
        
        if (_player == null) return;
        float mesafe = Vector3.Distance(transform.position, _player.transform.position);
            agent.SetDestination(_player.transform.position);

        if(state.TryAttack(mesafe,Time.deltaTime))
        {
            agent.isStopped = true;
            _animator.SetBool("isattack", true);
            _player.GetComponent<PlayerScript>().TakeDamage(state.attackDamage);
        }
        else
        {
            agent.isStopped = false;
            _animator.SetBool("isattack", false);
        }
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
