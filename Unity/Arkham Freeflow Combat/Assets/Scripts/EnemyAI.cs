using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public List<Enemy> enemiesList = new List<Enemy>();
    public Enemy attacker;
    public float swapModeTime = 2f;


    void Awake()
    {
        GameObject[] hostiles = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject hostile in hostiles)
        {
            enemiesList.Add(hostile.GetComponent<Enemy>());
        }
    }

    // Update is called once per frame
    void Start()
    {
        
    }

    private void Update()
    {
        if (Random.value <= (1/swapModeTime) * Time.deltaTime)
        {
            Enemy swapMode = enemiesList[Random.Range(0, enemiesList.Count)];
            if (swapMode.state == Enemy.EnemyState.Idle)
                swapMode.Circle();
            else if (swapMode.state == Enemy.EnemyState.Circulating)
                swapMode.state = Enemy.EnemyState.Idle;
        }
    }

    public void Countered()
    {

    }

    public void AttackCompleted()
    {

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
