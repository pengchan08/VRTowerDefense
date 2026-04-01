using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneManager : MonoBehaviour
{
    // 랜덤 시간 범위
    public float minTime = 1f;
    public float maxTime = 5f;
    private float createTime;
    private float currentTime;

    public Transform[] spawnPoints;
    public GameObject droneFactory;

    void Start()
    {
        createTime = Random.Range(minTime, maxTime);
    }

    void Update()
    {
        currentTime += Time.deltaTime;
        if (currentTime > createTime)
        {
            GameObject drone = Instantiate(droneFactory);
            int index = Random.Range(0, spawnPoints.Length);
            drone.transform.position = spawnPoints[index].position;
            currentTime = 0;
            createTime = Random.Range(minTime, maxTime);
        }
    }
}
