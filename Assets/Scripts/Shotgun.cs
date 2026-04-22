using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun : MonoBehaviour
{
    public Transform bulletImpact;
    private ParticleSystem bulletEffect;

    [Header("Muzzle Flash")]
    public ParticleSystem muzzleFlash;

    [Header("Sound")]
    public AudioClip fireClip;      // 발사 사운드 클립
    public AudioClip reloadClip;    // 재장전 사운드 클립

    private AudioSource fireAudio;   // 발사 전용 AudioSource
    private AudioSource reloadAudio; // 재장전 전용 AudioSource

    // Crosshair를 위한 속성
    public Transform crosshair;

    // 펠릿 1발당 피해량
    private int damagePerPellet = 2;
    // 한 번 발사 시 나가는 펠릿 수
    private int pelletCount = 4;
    // 현재 탄창 / 최대 탄창
    private int currentAmmo;
    private int maxAmmo = 5;
    // 재장전 시간 (초)
    private float reloadTime = 2.5f;
    // 공격 속도: 발사 후 다음 발사까지의 최소 간격 (초)
    private float fireRate = 1.0f;

    // 펠릿 퍼짐 각도 (도) — 값이 클수록 더 넓게 퍼짐
    private float spreadAngle = 1f;

    private Vector3[] pelletDirections = new Vector3[]
    {
        new Vector3( 0.00f,  0.00f, 1f),  // 중앙
        new Vector3( 0.05f,  0.00f, 1f),  // 오른쪽
        new Vector3(-0.05f,  0.00f, 1f),  // 왼쪽
        new Vector3( 0.00f,  0.05f, 1f),  // 위
        new Vector3( 0.00f, -0.05f, 1f),  // 아래
    };

    private bool isReloading = false;
    private float lastFireTime = -999f;

    private MeshRenderer[] gunMeshRenderers;

    public int CurrentAmmo  => currentAmmo;
    public int MaxAmmo      => maxAmmo;
    public bool IsReloading => isReloading;
    public bool IsCoolingDown => !isReloading && (Time.time - lastFireTime < fireRate);

    void Start()
    {
        bulletEffect = bulletImpact.GetComponent<ParticleSystem>();

        // ─── AudioSource 두 개를 이 GameObject에 자동 추가 ───
        AudioSource[] sources = GetComponents<AudioSource>();

        // 첫 번째 AudioSource → 발사용
        fireAudio = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        fireAudio.playOnAwake = false;
        fireAudio.clip = fireClip;

        // 두 번째 AudioSource → 재장전용
        reloadAudio = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        reloadAudio.playOnAwake = false;
        reloadAudio.clip = reloadClip;

        currentAmmo = maxAmmo;

        gunMeshRenderers = GetComponentsInChildren<MeshRenderer>(true);
    }

    void Update()
    {
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

        ARAVRInput.PlayVibration(0.15f, 0.3f, 1.0f, ARAVRInput.Controller.RTouch);

        // 총구 이펙트 재생
        if (muzzleFlash != null)
        {
            muzzleFlash.Stop();
            muzzleFlash.Play();
        }

        // 발사 사운드 재생
        if (fireAudio != null && fireClip != null)
        {
            fireAudio.Stop();
            fireAudio.Play();
        }

        int playerLayer = 1 << LayerMask.NameToLayer("Player");
        int towerLayer  = 1 << LayerMask.NameToLayer("Tower");
        int layerMask   = playerLayer | towerLayer;

        foreach (Vector3 localDir in pelletDirections)
        {
            Vector3 worldDir = TransformPelletDirection(localDir);
            Ray ray = new Ray(ARAVRInput.RHandPosition, worldDir);
            RaycastHit hitInfo;

            if (Physics.Raycast(ray, out hitInfo, 200, ~layerMask))
            {
                // 펠릿마다 이펙트 오브젝트를 복사해서 개별 위치에 재생
                GameObject impactObj = Instantiate(
                    bulletImpact.gameObject,
                    hitInfo.point,
                    Quaternion.LookRotation(hitInfo.normal)
                );
                ParticleSystem ps = impactObj.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop();
                    ps.Play();
                    // 파티클 재생이 끝나면 자동 제거
                    Destroy(impactObj, ps.main.duration + ps.main.startLifetime.constantMax);
                }
                else
                {
                    Destroy(impactObj, 1f);
                }

                DroneAI drone = hitInfo.transform.GetComponent<DroneAI>()
                             ?? hitInfo.transform.GetComponentInParent<DroneAI>();
                if (drone != null)
                {
                    drone.OnDamageProcess(damagePerPellet);
                }
            }
        }

        // 총구 이펙트는 원본 bulletImpact에서 한 번만 재생
        bulletEffect.Stop();
        bulletEffect.Play();

        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
        }
    }

    private Vector3 TransformPelletDirection(Vector3 localDir)
    {
        Vector3 forward = ARAVRInput.RHandDirection.normalized;
        Vector3 right   = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 up      = Vector3.Cross(forward, right).normalized;

        Vector3 baseDir = (forward * localDir.z
                         + right   * localDir.x
                         + up      * localDir.y).normalized;

        // 펠릿마다 랜덤 퍼짐 추가
        float randomX = Random.Range(-spreadAngle, spreadAngle);
        float randomY = Random.Range(-spreadAngle, spreadAngle);
        Quaternion spreadRot = Quaternion.Euler(randomX, randomY, 0);

        return spreadRot * baseDir;
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

        // 재장전 사운드 재생
        if (reloadAudio != null && reloadClip != null)
        {
            reloadAudio.Stop();
            reloadAudio.Play();
        }

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;

        SetGunVisible(true);

        Debug.Log("[Shotgun] 재장전 완료! 탄약: " + currentAmmo + " / " + maxAmmo);
    }

    public void OnWeaponDeactivate()
    {
        StopAllCoroutines();
        isReloading = false;

        // 무기 비활성화 시 재장전 사운드 중단
        if (reloadAudio != null) reloadAudio.Stop();

        SetGunVisible(true);
    }
}