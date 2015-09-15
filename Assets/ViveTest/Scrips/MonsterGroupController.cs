using UnityEngine;
using System.Collections;
using RootMotion.Demos;

public class MonsterGroupController : MonoBehaviour
{
    public int IntervalTime = 2000;
    public MonsterSpawner[] Spawners;
    public int LevelCount = 1;

    private int currentLevel = 0;
    private float nextGenerateTime;

    void Awake()
    {
        if (Spawners.Length == 0)
        {
            Spawners = this.transform.GetComponentsInChildren<MonsterSpawner>();
        }

        if (Spawners.Length > 0)
        {
            foreach (MonsterSpawner spawner in Spawners)
            {
                spawner.IntervalTime = -1;
            }
        }
    }

    void Start()
    {
        StartSpawnerLevel();

    }

    void Update()
    {
        if (Time.time < nextGenerateTime)
        {
            currentLevel++;
            if (LevelCount > currentLevel)
            {
                StartSpawnerLevel();
            }
        }
    }

    public void StartSpawnerLevel()
    {
        nextGenerateTime = Time.time + IntervalTime;

        foreach (MonsterSpawner spawner in Spawners)
        {
            spawner.StartGenerateLevel(currentLevel);
        }
    }
}
