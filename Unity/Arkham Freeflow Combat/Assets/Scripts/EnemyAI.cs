using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            player.GetComponent<Player>().Combo(2);
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

    IEnumerator EndCamera(Enemy enemy)
    {
        GameObject cam = Camera.main.gameObject;
        cam.transform.SetParent(null);
        Transform pointB = enemy.transform;
        Transform pointC = player.transform;
        float sideLength = 5f;
        //Vector3 perpDir = Vector3.Cross(Vector3.up, player.transform.position - enemy.transform.position).normalized;
        Vector3 B = pointB.position; B.y = 0;
        Vector3 C = pointC.position; C.y = 0;
        Vector3 M = (B + C) * 0.5f;
        Vector3 BCdir = (C - B).normalized;
        float baseLength = Vector3.Distance(B, C);
        float halfBase = baseLength * 0.5f;
        float height = Mathf.Sqrt(Mathf.Max(0, sideLength * sideLength - halfBase * halfBase));
        Vector3 perpDir = Vector3.Cross(Vector3.up, BCdir).normalized;
        Vector3 A1 = M + perpDir * height;
        Vector3 A2 = M - perpDir * height;
        Vector3 chosen = (Vector3.Distance(cam.transform.position, A1) < Vector3.Distance(cam.transform.position, A2)) ? A1 : A2;
        chosen.y = 1.5f;

        Vector3 camStart = cam.transform.position;
        float startTime = Time.unscaledTime;
        float duration = 3f;
        while (Time.unscaledTime < startTime + duration)
        {
            cam.transform.position = Vector3.Slerp(camStart, chosen, (Time.unscaledTime - startTime) / duration);
            Time.timeScale = Mathf.Lerp(Time.timeScale, 0.2f, (Time.unscaledTime - startTime) / duration);
            Quaternion targetRotation = Quaternion.LookRotation(M - cam.transform.position);
            cam.transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.unscaledDeltaTime);
            yield return null;
        }
        startTime = Time.unscaledTime;
        while (Time.unscaledTime < startTime + duration/2)
        {
            Time.timeScale = Mathf.Lerp(Time.timeScale, 1f, (Time.unscaledTime - startTime) / duration);
            yield return null;
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

    public void CheckCount(Enemy enemy)
    {
        if (enemiesList.Count == 0)
        {
            StartCoroutine(EndCamera(enemy));
        }
    }

    IEnumerator DelayFunction(float time)
    {
        enabled = false;
        yield return new WaitForSeconds(time);
        enabled = true;
    }
}
