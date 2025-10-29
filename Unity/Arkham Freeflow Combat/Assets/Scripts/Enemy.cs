using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    CharacterController controller;
    GameObject player;
    Animator animator;

    public EnemyState state = EnemyState.Idle;
    public float health = 3;
    bool attacking = false;
    bool retreating = false;
    int clockwise = 1;
    Vector2 movement;
    float acceleration = 1f;
    static float speed = 1.5f;

    private float rayDistance = 1f;
    public LayerMask avoidWhileCirculating;
    public LayerMask playerMask;

    static int minDistance = 3;
    static int maxDistance = 8;
    float targetDistance = 4;

    float stunStartTime;
    static float stunDuration = 0.8f;

    public GameObject counterIcon;

    public enum EnemyState
    {
        Idle,
        Attacking,
        Retreating,
        Circulating,
        Stunned,
        Dead
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        player = GameObject.FindWithTag("Player");

        clockwise = Random.value < 0.5 ? -1 : 1;
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
            case EnemyState.Stunned:
                Stunned();
                break;
            case EnemyState.Dead:
                break;
        }

        animator.speed = 1f + movement.magnitude / 2f;

        
    }

    float DistanceToPlayer()
    {
        return Vector3.Distance(transform.position, player.transform.position);
    }

    void Idle()
    {
        transform.forward = player.transform.position - transform.position;
        Move(0, 0);

        float distance = DistanceToPlayer();
        if (distance < minDistance || distance > maxDistance) Retreat();
    }

    void Attacking()
    {
        targetDistance = 2f;
        transform.forward = player.transform.position - transform.position;
        float distance = DistanceToPlayer();
        Move(0, distance - targetDistance, 2f);
    }

    void Retreating()
    {
        transform.forward = player.transform.position - transform.position;
        float distance = DistanceToPlayer();
        if (distance > targetDistance + 0.1f) Move(0, 1);
        else if (distance < targetDistance - 0.1f) Move(0, -1f);
        else
        {
            if (Random.value < 0.5f) Circle();
            else state = EnemyState.Idle;
        }
    }

    public void Retreat()
    {
        targetDistance = Random.Range(minDistance, maxDistance);
        state = EnemyState.Retreating;
    }

    void Circulating()
    {
        transform.forward = player.transform.position - transform.position;

        float distance = DistanceToPlayer();
        if (distance < minDistance || distance > maxDistance)
        {
            Retreat();
            return;
        }

        int moveCloser = distance > targetDistance? 1 : -1;
        Move(clockwise, CheckObstacles() * 10f + moveCloser);
    }

    private float CheckObstacles()
    {
        Vector3 origin = transform.position;

        // Directions
        Vector3 front = (transform.forward / 4 + transform.right * clockwise).normalized;
        Vector3 back = (-transform.forward / 6 + transform.right * clockwise).normalized;

        RaycastHit frontHit;
        RaycastHit backHit;

        bool f = Physics.SphereCast(origin + transform.up + transform.right * clockwise, 0.5f, front, out frontHit, rayDistance, avoidWhileCirculating);
        bool b = Physics.SphereCast(origin + transform.up + transform.right * clockwise, 0.5f, back, out backHit, rayDistance, avoidWhileCirculating);

        // Neither hit
        if (!f && !b)
            return 0;

        // Only front hit
        if (f && !b)
            return -1;

        // Only back hit
        if (!f && b)
            return +1;

        // Both hit compare distances
        if (backHit.distance < 0.2f || frontHit.distance < 0.2f)
            clockwise *= -1;
        else
            return (frontHit.distance - backHit.distance)/rayDistance;
        return 0;
    }

    public void Attack()
    {
        animator.SetTrigger("Attack");
        SetCounterIcon(true);
    }

    public void SetCounterIcon(bool active)
    {
        counterIcon.SetActive(active);
    }

    public void Circle()
    {
        clockwise = Random.value < 0.5f ? -1 : 1;
        state = EnemyState.Circulating;
    }

    public void IsHit(int dmg)
    {
        health -= dmg;
        EnemyAI.Instance.Hit(this);
        SetCounterIcon(false);
        if (health <= 0)
        {
            Die();
            return;
        }
        Stun();
        animator.SetTrigger("Hit");
    }

    void Stunned()
    {
        Move(0, 0);
        if (Time.time > stunStartTime + stunDuration)
        {
            Retreat();
        }
    }

    public void Stun()
    {
        stunStartTime = Time.time;
        movement = new Vector2(0, 0);
        state = EnemyState.Stunned;
    }

    void Hit()
    {
        RaycastHit hit;
        if (Vector3.Distance(player.transform.position, transform.position) < 3f)
        {
            if (!EnemyAI.Instance.IsCountered())
            {
                player.GetComponent<Player>().IsHit();
                EnemyAI.Instance.attackers.Remove(this);
            } 
            else
                IsHit(2);
        }
        else
        {
            if (EnemyAI.Instance.IsCountered())
                IsHit(2);
        }
        SetCounterIcon(false);
        state = EnemyState.Retreating;
        EnemyAI.Instance.attackers.Remove(this);
    }

    public void Die()
    {
        animator.SetTrigger("Die");
        state = EnemyState.Dead;
        EnemyAI.Instance.Killed(this);
        this.enabled = false;
        Destroy(controller);
        Destroy(GetComponent<Rigidbody>());
        Destroy(this);
    }

    private void Move(float x, float y, float speedMult = 1f)
    {
        Vector2 input = new Vector2(x, y).normalized;
        
        movement = Vector2.MoveTowards(movement, new Vector2(input.x, input.y), acceleration * Time.deltaTime);
        controller.Move((transform.right * movement.x + transform.forward * movement.y) * speed * speedMult * Time.deltaTime);

        animator.SetFloat("MoveX", Vector3.Dot(controller.velocity, transform.right));
        animator.SetFloat("MoveY", Vector3.Dot(controller.velocity, transform.forward));

        //animator.SetFloat("MoveX", movement.x);
        //animator.SetFloat("MoveY", movement.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (state != EnemyState.Circulating) return;
        Vector3 origin = transform.position;
        Gizmos.DrawRay(origin + transform.up + transform.right * clockwise, (transform.forward / 4 + transform.right * clockwise).normalized * rayDistance);
        Gizmos.DrawRay(origin + transform.up + transform.right * clockwise, (-transform.forward / 6 + transform.right * clockwise).normalized * rayDistance);
    }
}
