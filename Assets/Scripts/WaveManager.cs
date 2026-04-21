using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    [Header("Drone Prefabs")]
    public GameObject dronePrefab;
    public GameObject bossDronePrefab;

    [Header("Spawn Points")]
    public Transform[] droneSpawnPoints;
    public Transform   droneTarget;

    [Header("Bomb")]
    public GameObject  bombPrefab;
    public Transform[] bombSpawnPoints;

    [Header("Wave Announcement UI")]
    public Transform announcementUI;
    public Text      announcementText;
    public float     announceHoldTime = 1.5f;
    public float     announceFadeTime = 1.0f;

    private int[]   waveDroneCount    = { 3,    6,    10,   15,   3   };
    private float[] waveSpawnInterval = { 3f,   2.5f, 2f,   1.5f, 2f  };
    private float[] waveDelay         = { 5f,   5f,   5f,   5f,   20f };

    private const int BOSS_WAVE   = 5;
    private const int TOTAL_WAVES = 5;

    private int currentWave = 0;
    private int aliveDrones = 0;

    void Start()
    {
        float z = Camera.main.nearClipPlane + 0.5f;
        announcementUI.SetParent(Camera.main.transform);
        announcementUI.localPosition = new Vector3(0f, 0.1f, z);
        announcementUI.localRotation = Quaternion.identity;
        announcementUI.gameObject.SetActive(false);

        StartCoroutine(WaveLoop());
    }

    private IEnumerator WaveLoop()
    {
        while (true)
        {
            int waveIndex  = currentWave;
            int waveNumber = waveIndex + 1;
            bool isBossWave = (waveNumber == BOSS_WAVE);

            // 1. 웨이브 시작 알림
            string msg = isBossWave
                ? "<color=red>Boss Wave!</color>"
                : $"Wave {waveNumber}";
            yield return StartCoroutine(ShowAnnouncement(msg));

            // 2. 폭탄 생성
            SpawnBombs();

            // 3. 드론 스폰
            aliveDrones = 0;

            int   count    = waveDroneCount[waveIndex];
            float interval = waveSpawnInterval[waveIndex];

            if (isBossWave)
            {
                SpawnDrone(true);
                yield return new WaitForSeconds(interval);

                for (int i = 0; i < count - 1; i++)
                {
                    SpawnDrone(false);
                    yield return new WaitForSeconds(interval);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    SpawnDrone(false);
                    yield return new WaitForSeconds(interval);
                }
            }

            // 4. 모든 드론이 죽을 때까지 대기 (매 프레임 체크)
            yield return new WaitUntil(() => aliveDrones <= 0);

            // 5. 웨이브 클리어 알림
            if (waveNumber >= TOTAL_WAVES)
            {
                yield return StartCoroutine(ShowAnnouncement("<color=yellow>Mission Complete!</color>"));
                yield return new WaitForSeconds(waveDelay[TOTAL_WAVES - 1]);
                currentWave = 0;
            }
            else
            {
                yield return StartCoroutine(ShowAnnouncement($"Wave {waveNumber} Clear!"));
                yield return new WaitForSeconds(waveDelay[waveIndex]);
                currentWave++;
            }
        }
    }

    private void SpawnDrone(bool isBoss)
    {
        if (droneSpawnPoints == null || droneSpawnPoints.Length == 0) return;

        Transform  spawnPoint = droneSpawnPoints[Random.Range(0, droneSpawnPoints.Length)];
        GameObject prefab     = (isBoss && bossDronePrefab != null) ? bossDronePrefab : dronePrefab;
        GameObject obj        = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        DroneAI drone = obj.GetComponent<DroneAI>();
        if (drone != null)
        {
            drone.isBoss = isBoss;
            drone.target = droneTarget;
        }

        aliveDrones++;

        DroneDeathNotifier notifier = obj.AddComponent<DroneDeathNotifier>();
        notifier.onDeath = () => aliveDrones--;
    }

    private void SpawnBombs()
    {
        if (bombPrefab == null || bombSpawnPoints == null || bombSpawnPoints.Length == 0) return;

        int bombCount = Random.Range(1, 3);

        List<int> indices = new List<int>();
        for (int i = 0; i < bombSpawnPoints.Length; i++) indices.Add(i);

        for (int i = 0; i < bombCount && i < indices.Count; i++)
        {
            int randIdx = Random.Range(i, indices.Count);
            int tmp = indices[i]; indices[i] = indices[randIdx]; indices[randIdx] = tmp;
            Instantiate(bombPrefab, bombSpawnPoints[indices[i]].position, bombSpawnPoints[indices[i]].rotation);
        }
    }

    private IEnumerator ShowAnnouncement(string message)
    {
        announcementText.text = message;

        Color c = announcementText.color;
        c.a = 1f;
        announcementText.color = c;
        announcementUI.gameObject.SetActive(true);

        yield return new WaitForSeconds(announceHoldTime);

        float elapsed = 0f;
        while (elapsed < announceFadeTime)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / announceFadeTime);
            announcementText.color = c;
            yield return null;
        }

        announcementUI.gameObject.SetActive(false);
    }

    public int CurrentWave => currentWave + 1;
}