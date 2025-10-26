using UnityEngine;

public class Enemy : MonoBehaviour
{
    CharacterController controller;
    GameObject player;
    Animator animator;

    public EnemyState state = EnemyState.Idle;
    float health = 3;
    bool attacking = false;
    bool retreating = false;
    int clockwise;
    Vector2 movement;
    public float acceleration = 1f;

    public enum EnemyState
    {
        Idle,
        Attacking,
        Retreating,
        Circulating,
        Stunned
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        player = GameObject.FindWithTag("Player");

        clockwise = Random.value < 0.5f ? -1 : 1;
    }

    private void Update()
    {
        switch(state)
        {
            case EnemyState.Idle:
                Idle();
                break;
            case EnemyState.Attacking:
                if (!attacking) Attacking();
                break;
            case EnemyState.Retreating:
                if (!retreating) Retreating();
                Retreating();
                break;
            case EnemyState.Circulating:
                Circulating();
                break;
        }

        
    }

    void Idle()
    {
        transform.forward = player.transform.position - transform.position;
        Move(0, 0);
    }

    void Attacking()
    {
        transform.forward = player.transform.position - transform.position;
        Move(0, 1);
    }

    void Retreating()
    {
        transform.forward = player.transform.position - transform.position;
        Move(0, -0.7f);
    }

    void Circulating()
    {
        transform.forward = player.transform.position - transform.position;
        Move(clockwise, 0);
    }

    public void Attack()
    {
        state = EnemyState.Attacking;
        animator.SetInteger("AttackNum", Random.Range(0, 3));
        animator.SetTrigger("Attack");
    }
    public void Hit(int dmg)
    {
        health -= dmg;
        if (health <= 0)
        {
            Die();
            return;
        }
        state = EnemyState.Stunned;
        animator.SetTrigger("Hit");
    }
    public void Die()
    {
        animator.SetTrigger("Die");
        this.enabled = false;
    }

    private void Move(float x, float y)
    {
        movement = Vector2.MoveTowards(movement, new Vector2(x, y), acceleration * Time.deltaTime);
        controller.Move((transform.right * movement.x + transform.forward * movement.y) * Time.deltaTime);

        animator.SetFloat("MoveX", Vector3.Dot(controller.velocity, transform.right));
        animator.SetFloat("MoveY", Vector3.Dot(controller.velocity, transform.forward));

        //animator.SetFloat("MoveX", movement.x);
        //animator.SetFloat("MoveY", movement.y);
    }
}
