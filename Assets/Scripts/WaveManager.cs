using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    // 드론 프리팹
    [Header("Drone Prefabs")]
    public GameObject dronePrefab;
    public GameObject bossDronePrefab;  // 없으면 dronePrefab에 isBoss=true 적용

    // 스폰 위치
    [Header("Spawn Points")]
    public Transform[] droneSpawnPoints;  // 드론 스폰 위치
    public Transform   droneTarget;       // 드론이 향할 목표 (타워)

    // 폭탄
    [Header("Bomb")]
    public GameObject bombPrefab;
    // 폭탄이 생성될 오른쪽 대각선 위치들
    public Transform[] bombSpawnPoints;

    // 웨이브 알림 UI
    [Header("Wave Announcement UI")]
    public Transform announcementUI;
    public Text      announcementText;
    public float     announceHoldTime = 1.5f;
    public float     announceFadeTime = 1.0f;

    // 웨이브별 드론 수 / 생성 간격 / 웨이브 후 대기 시간
    private int[]   waveDroneCount    = { 3,    6,    10,   15,   3   };
    private float[] waveSpawnInterval = { 3f,   2.5f, 2f,   1.5f, 2f  };
    private float[] waveDelay         = { 5f,   5f,   5f,   5f,   20f };

    private const int BOSS_WAVE      = 5;   // 보스 웨이브 번호 (1-based)
    private const int TOTAL_WAVES    = 5;

    private int  currentWave  = 0;          // 0-based 인덱스
    private int  aliveDrones  = 0;
    private bool isSpawning   = false;
    private bool isWaiting    = false;      // 웨이브 간 대기 중 여부

    public int CurrentWave => currentWave;

    void Start()
    {
        // 웨이브 알림 UI를 카메라 앞에 고정
        float z = Camera.main.nearClipPlane + 0.5f;
        announcementUI.SetParent(Camera.main.transform);
        announcementUI.localPosition = new Vector3(0f, 0.1f, z);
        announcementUI.localRotation = Quaternion.identity;
        announcementUI.gameObject.SetActive(false);

        StartCoroutine(RunWave());
    }

    void Update()
    {
        // 스폰 완료 & 드론 전멸 & 대기 중 아닐 때 → 웨이브 종료 처리
        if (!isSpawning && !isWaiting && aliveDrones <= 0 && currentWave > 0)
        {
            isWaiting = true;   // 중복 호출 방지
            StartCoroutine(WaveClear());
        }
    }

    // ─── 웨이브 실행 ─────────────────────────────────
    private IEnumerator RunWave()
    {
        int waveIndex = currentWave;            // 0-based
        int waveNumber = waveIndex + 1;         // 표시용 1-based
        bool isBossWave = (waveNumber == BOSS_WAVE);

        // 웨이브 시작 알림
        string msg = isBossWave
            ? "<color=red>Boss Wave!</color>"
            : $"Wave {waveNumber}";
        yield return StartCoroutine(ShowAnnouncement(msg));

        // 폭탄 1~2개 생성
        SpawnBombs();

        // 드론 스폰
        isSpawning = true;
        aliveDrones = 0;

        int   count    = waveDroneCount[waveIndex];
        float interval = waveSpawnInterval[waveIndex];

        if (isBossWave)
        {
            // 보스 웨이브: 보스 1마리 + 일반 드론 2마리 = 총 3마리
            SpawnDrone(true);   // 보스
            aliveDrones++;
            yield return new WaitForSeconds(interval);

            for (int i = 0; i < count - 1; i++)   // 나머지 2마리는 일반
            {
                SpawnDrone(false);
                aliveDrones++;
                yield return new WaitForSeconds(interval);
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                SpawnDrone(false);
                aliveDrones++;
                yield return new WaitForSeconds(interval);
            }
        }

        isSpawning = false;
        currentWave++;
    }

    // ─── 웨이브 클리어 ───────────────────────────────
    private IEnumerator WaveClear()
    {
        int finishedWave = currentWave;   // 방금 끝난 웨이브 (1-based)

        if (finishedWave >= TOTAL_WAVES)
        {
            // 5웨이브 클리어 → Mission Complete 후 20초 대기, 1웨이브로 복귀
            yield return StartCoroutine(ShowAnnouncement("<color=yellow>Mission Complete!</color>"));
            yield return new WaitForSeconds(waveDelay[TOTAL_WAVES - 1]);  // 20초

            currentWave = 0;
        }
        else
        {
            // 일반 웨이브 클리어
            yield return StartCoroutine(ShowAnnouncement($"Wave {finishedWave} Clear!"));
            yield return new WaitForSeconds(waveDelay[finishedWave - 1]);
        }

        isWaiting = false;
        StartCoroutine(RunWave());
    }

    // ─── 드론 스폰 ───────────────────────────────────
    private void SpawnDrone(bool isBoss)
    {
        if (droneSpawnPoints == null || droneSpawnPoints.Length == 0) return;

        Transform spawnPoint = droneSpawnPoints[Random.Range(0, droneSpawnPoints.Length)];

        GameObject prefab = (isBoss && bossDronePrefab != null) ? bossDronePrefab : dronePrefab;
        GameObject obj    = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        DroneAI drone = obj.GetComponent<DroneAI>();
        if (drone != null)
        {
            drone.isBoss = isBoss;
            drone.target = droneTarget;
        }

        // 드론 사망 시 카운트 감소
        DroneDeathNotifier notifier = obj.AddComponent<DroneDeathNotifier>();
        notifier.onDeath = () => aliveDrones--;
    }

    // ─── 폭탄 스폰 ───────────────────────────────────
    private void SpawnBombs()
    {
        if (bombPrefab == null || bombSpawnPoints == null || bombSpawnPoints.Length == 0) return;

        int bombCount = Random.Range(1, 3);   // 1 또는 2개

        // 스폰 포인트를 섞어서 중복 없이 선택
        List<int> indices = new List<int>();
        for (int i = 0; i < bombSpawnPoints.Length; i++) indices.Add(i);

        for (int i = 0; i < bombCount && i < indices.Count; i++)
        {
            int randIdx = Random.Range(i, indices.Count);
            int tmp = indices[i]; indices[i] = indices[randIdx]; indices[randIdx] = tmp;

            Transform sp = bombSpawnPoints[indices[i]];
            Instantiate(bombPrefab, sp.position, sp.rotation);
        }
    }

    // ─── 웨이브 알림 연출 ────────────────────────────
    private IEnumerator ShowAnnouncement(string message)
    {
        announcementText.text = message;

        Color c = announcementText.color;
        c.a = 1f;
        announcementText.color = c;
        announcementUI.gameObject.SetActive(true);

        yield return new WaitForSeconds(announceHoldTime);

        // 페이드 아웃
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
}