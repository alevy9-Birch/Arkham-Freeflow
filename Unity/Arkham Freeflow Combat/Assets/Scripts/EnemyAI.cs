using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public List<Enemy> enemiesList = new List<Enemy>();
    public List<Enemy> attackers;
    public float swapModeTime = 1f;
    public int counterCount;

    Coroutine attackController;
    public static EnemyAI Instance;
    public float attackDelay = 2.5f;
    public bool counterable = false;
    GameObject player;


    void Awake()
    {
        player = GameObject.FindWithTag("Player");

        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
        
        GameObject[] hostiles = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject hostile in hostiles)
        {
            enemiesList.Add(hostile.GetComponent<Enemy>());
        }
    }

    private void Update()
    {
        if (enemiesList.Count <= 0)
        {
            enabled = false;
            return;
        }
        
        if (Random.value <= (1/swapModeTime) * Time.deltaTime)
        {
            Enemy swapMode = enemiesList[Random.Range(0, enemiesList.Count)];
            if (swapMode.state == Enemy.EnemyState.Idle)
                swapMode.Circle();
            else if (swapMode.state == Enemy.EnemyState.Circulating)
                swapMode.state = Enemy.EnemyState.Idle;
        }

        if (attackers.Count <= 0)
        {
            attackController = null;
            Attack(4 - Mathf.CeilToInt(Mathf.Sqrt(Random.Range(0.001f, 9f))));
            counterCount = 0;
        }
    }

    public void Attack(int numOfAttackers)
    {
        for (int i = 0; i < numOfAttackers; i++)
        {
            Enemy enemy = enemiesList[Random.Range(0, enemiesList.Count)];
            if (enemy.state != Enemy.EnemyState.Stunned && Vector3.Distance(enemy.transform.position, player.transform.position) < enemy.attackRange)
            {
                attackers.Add(enemy);
                enemy.state = Enemy.EnemyState.Attacking;
                
            }
        }

        attackController = StartCoroutine(AttackController());
    }

    IEnumerator AttackController()
    {
        yield return new WaitForSeconds(attackDelay);
        counterCount = 0;
        counterable = true;
        foreach (Enemy enemy in attackers)
        {
            enemy.Attack();
        }
    }

    public bool IsCountered()
    {
        counterable = false;
        if (counterCount > 0)
        {
            counterCount--;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Counter()
    {
        counterCount++;
        if (counterCount <= attackers.Count)
        {
            attackers[counterCount - 1].SetCounterIcon(false);
        }
    }

    public void Hit(Enemy enemy)
    {
        attackers.Remove(enemy);
        if (attackers.Count <= 0)
        {
            StopCoroutine(attackController);
            attackController = null;
        }
    }

    public void Killed(Enemy enemy)
    {
        enemiesList.Remove(enemy);
    }

    public void EnemyDefeated(Enemy enemy)
    {
        enemiesList.Remove(enemy);
    }

    void WaitTime(float time)
    {
        StartCoroutine(DelayFunction(time));
    }

    IEnumerator DelayFunction(float time)
    {
        enabled = false;
        yield return new WaitForSeconds(time);
        enabled = true;
    }
}
