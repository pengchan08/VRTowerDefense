using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun : MonoBehaviour
{
    public Transform bulletImpact;
    private ParticleSystem bulletEffect;
    private AudioSource bulletAudio;
    // Crosshair를 위한 속성
    public Transform crosshair;

    // 1발당 피해량
    private int damagePerPellet = 3;
    // 한 번 발사 시 나가는 펠릿 수
    private int pelletCount = 5;
    // 현재 탄창 / 최대 탄창
    private int currentAmmo;
    private int maxAmmo = 12;
    // 재장전 시간 (초)
    private float reloadTime = 2.0f;
    // 공격 속도: 발사 후 다음 발사까지의 최소 간격 (초)
    private float fireRate = 1.2f;

    // 5방향 로컬 방향 오프셋 (중앙 1 + 상하좌우 4)
    // 거리가 멀수록 퍼짐이 커지도록 Ray에 각도를 부여
    private Vector3[] pelletDirections = new Vector3[]
    {
        new Vector3( 0.00f,  0.00f, 1f),   // 중앙
        new Vector3( 0.06f,  0.00f, 1f),   // 오른쪽
        new Vector3(-0.06f,  0.00f, 1f),   // 왼쪽
        new Vector3( 0.00f,  0.06f, 1f),   // 위
        new Vector3( 0.00f, -0.06f, 1f),   // 아래
    };

    // 재장전 중 여부
    private bool isReloading = false;
    // 마지막 발사 시각 (Time.time 기준)
    private float lastFireTime = -999f;

    // 재장전 중 숨길 MeshRenderer 목록 (자동 수집)
    private MeshRenderer[] gunMeshRenderers;

    void Start()
    {
        // 총알 효과 파티클 시스템 컴포넌트 가져오기
        bulletEffect = bulletImpact.GetComponent<ParticleSystem>();
        // 총알 효과 오디오 소스 컴포넌트 가져오기
        bulletAudio = bulletImpact.GetComponent<AudioSource>();

        // 탄창 초기화
        currentAmmo = maxAmmo;

        // 자식 오브젝트의 MeshRenderer 전부 수집
        gunMeshRenderers = GetComponentsInChildren<MeshRenderer>(true);
    }

    void Update()
    {
        // 크로스헤어 표시
        ARAVRInput.DrawCrosshair(crosshair);

        if (isReloading) return;

        bool fireInput = ARAVRInput.GetDown(ARAVRInput.Button.IndexTrigger)
                         || Input.GetKeyDown(KeyCode.LeftShift)
                         || Input.GetKeyDown(KeyCode.RightShift);

        if (fireInput)
        {
            if (currentAmmo <= 0)
            {
                StartCoroutine(Reload());
                return;
            }

            if (Time.time - lastFireTime < fireRate) return;

            Fire();
        }
    }

    private void Fire()
    {
        lastFireTime = Time.time;
        currentAmmo--;

        // VR 컨트롤러 진동 재생
        ARAVRInput.PlayVibration(ARAVRInput.Controller.RTouch);

        // 총알 오디오 재생
        bulletAudio.Stop();
        bulletAudio.Play();

        // 플레이어 / 타워 레이어 마스크
        int playerLayer = 1 << LayerMask.NameToLayer("Player");
        int towerLayer  = 1 << LayerMask.NameToLayer("Tower");
        int layerMask   = playerLayer | towerLayer;

        bool hitSomething = false;

        foreach (Vector3 localDir in pelletDirections)
        {
            // 오른손 방향 기준으로 로컬 오프셋을 월드 방향으로 변환
            Vector3 worldDir = ARAVRInput.RHandDirection
                + ARAVRInput.RHandPosition != Vector3.zero
                ? TransformPelletDirection(localDir)
                : localDir;

            Ray ray = new Ray(ARAVRInput.RHandPosition, worldDir);
            RaycastHit hitInfo;

            if (Physics.Raycast(ray, out hitInfo, 200, ~layerMask))
            {
                // 가장 마지막으로 충돌한 지점에 이펙트 표시
                hitSomething = true;
                bulletImpact.position = hitInfo.point;
                bulletImpact.forward  = hitInfo.normal;

                if (hitInfo.transform.name.Contains("Drone"))
                {
                    DroneAI drone = hitInfo.transform.GetComponent<DroneAI>();
                    if (drone)
                    {
                        drone.OnDamageProcess(damagePerPellet);
                    }
                }
            }
        }

        if (hitSomething)
        {
            bulletEffect.Stop();
            bulletEffect.Play();
        }

        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
        }
    }

    // 오른손 방향(forward)을 기준으로 로컬 오프셋을 월드 방향으로 변환
    private Vector3 TransformPelletDirection(Vector3 localDir)
    {
        // RHand의 forward/right/up 벡터를 기준으로 월드 방향 계산
        Vector3 forward = ARAVRInput.RHandDirection.normalized;
        Vector3 right   = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 up      = Vector3.Cross(forward, right).normalized;

        return (forward * localDir.z
              + right   * localDir.x
              + up      * localDir.y).normalized;
    }

    private void SetGunVisible(bool visible)
    {
        if (gunMeshRenderers == null) return;
        foreach (MeshRenderer mr in gunMeshRenderers)
        {
            mr.enabled = visible;
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("[Shotgun] 재장전 중... (" + reloadTime + "초)");

        SetGunVisible(false);

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;

        SetGunVisible(true);

        Debug.Log("[Shotgun] 재장전 완료! 탄약: " + currentAmmo + " / " + maxAmmo);
    }

    public int CurrentAmmo  => currentAmmo;
    public int MaxAmmo      => maxAmmo;
    public bool IsReloading => isReloading;

    public void OnWeaponDeactivate()
    {
        StopAllCoroutines();
        isReloading = false;
        SetGunVisible(true);
    }
}