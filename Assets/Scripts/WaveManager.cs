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
    public string      bombTag   = "Bomb";
    private const int  MAX_BOMBS = 3;

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

        // 씬에 현재 남아있는 폭탄 수 확인
        int currentBombCount = GameObject.FindGameObjectsWithTag(bombTag).Length;
        int canSpawn = MAX_BOMBS - currentBombCount;

        // 이미 3개 이상이면 생성 안 함
        if (canSpawn <= 0)
        {
            Debug.Log("[WaveManager] 폭탄이 이미 최대(" + MAX_BOMBS + "개)입니다. 스폰 생략.");
            return;
        }

        // 이미 폭탄이 있는 스폰 포인트 제외 — 반경 0.5f 안에 폭탄 태그 오브젝트가 있으면 점유된 것으로 판단
        List<Transform> emptyPoints = new List<Transform>();
        foreach (Transform sp in bombSpawnPoints)
        {
            Collider[] nearby = Physics.OverlapSphere(sp.position, 0.5f);
            bool occupied = false;
            foreach (Collider col in nearby)
            {
                if (col.CompareTag(bombTag))
                {
                    occupied = true;
                    break;
                }
            }
            if (!occupied) emptyPoints.Add(sp);
        }

        // 빈 스폰 포인트가 없으면 생성 안 함
        if (emptyPoints.Count == 0) return;

        // 0~2개 랜덤, 남은 슬롯과 빈 포인트 수를 초과하지 않도록 제한
        int bombCount = Mathf.Min(Random.Range(0, 3), Mathf.Min(canSpawn, emptyPoints.Count));

        // 빈 포인트 목록을 셔플해서 중복 없이 선택
        for (int i = 0; i < emptyPoints.Count; i++)
        {
            int randIdx = Random.Range(i, emptyPoints.Count);
            Transform tmp = emptyPoints[i];
            emptyPoints[i] = emptyPoints[randIdx];
            emptyPoints[randIdx] = tmp;
        }

        for (int i = 0; i < bombCount; i++)
        {
            Instantiate(bombPrefab, emptyPoints[i].position, emptyPoints[i].rotation);
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