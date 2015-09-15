using UnityEngine;
using System.Collections;
using RootMotion.Demos;
using System;

public class MonsterSpawner : MonoBehaviour
{
    public float IntervalTime = 10;
    public MonsterGenerater[] GenerateDatas;

    private int currentLevel = 0;
    private MonsterGenerater currentGenerator;

    private float nextGenerateTime;
    
    void Start()
    {
        if (IntervalTime > 0)
        {
            StartGenerateLevel(currentLevel);
        }
    }

    void Update()
    {
        if (currentGenerator != null)
        {
            currentGenerator.Update();
        }

        if (IntervalTime > 0)
        {
            if (nextGenerateTime > 0 && Time.time > nextGenerateTime)
            {
                currentLevel++;
                //if (GenerateDatas.Length > currentLevel)
                {
                    StartGenerateLevel(currentLevel);
                }
            }
        }
    }

    public void StartGenerateLevel(int level)
    {
        if (currentGenerator != null)
            currentGenerator.Stop();

        if (GenerateDatas.Length <= level)
        {
            nextGenerateTime = -1;
            return;
        }
        nextGenerateTime = Time.time + IntervalTime;

        currentLevel = level;
        MonsterGenerater generator = GenerateDatas[level];

        currentGenerator = generator;

        generator.Start(this);
    }

    public Vector3 GetGeneratePosition()
    {
        return transform.position;
    }
}

[Serializable]
public class MonsterGenerater
{
    public float MinIntervalTime = 3.0f;
    public float MaxIntervalTime = 5.0f;
    //public float PriodTime = 10.0f;

    public MonsterController[] monsterPrefabs;

    private MonsterSpawner spawner;
    private float nextGenerateTime;

    private bool isStarted;

    public void Start(MonsterSpawner spawner)
    {
        this.spawner = spawner;

        updateNextGenerateTime();
    }

    public void Stop()
    {
        this.spawner = null;
        nextGenerateTime = -1f;
    }

    public void Update()
    {
        int count = monsterPrefabs.Length;

        if(count == 0)
            return;

        if (nextGenerateTime > 0 && Time.time > nextGenerateTime)
        {
            int index = (int)(UnityEngine.Random.value * count);

            MonsterController prefab = monsterPrefabs[index];

            MonsterController monster = GameObject.Instantiate<MonsterController>(prefab);

            monster.transform.position = spawner.GetGeneratePosition();

            updateNextGenerateTime();
        }
    }

    private void updateNextGenerateTime()
    {
        nextGenerateTime = Time.time + MinIntervalTime + UnityEngine.Random.value * (MaxIntervalTime - MinIntervalTime);
    }
}
