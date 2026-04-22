using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineGun : MonoBehaviour
{
    public Transform bulletImpact;
    private ParticleSystem bulletEffect;

    [Header("Sound")]
    public AudioClip fireClip;      // 발사 사운드 클립
    public AudioClip reloadClip;    // 재장전 사운드 클립

    private AudioSource fireAudio;   // 발사 전용 AudioSource
    private AudioSource reloadAudio; // 재장전 전용 AudioSource

    public Transform crosshair;

    // 1발당 피해량
    private int damage = 3;
    // 현재 탄창 / 최대 탄창
    private int currentAmmo;
    private int maxAmmo = 30;
    // 재장전 시간 (초)
    private float reloadTime = 3.0f;
    // 공격 속도: 발사 후 다음 발사까지의 최소 간격 (초)
    private float fireRate = 0.12f;

    // 탄퍼짐 최대 각도 (도)
    private float spreadAngle = 3f;

    // 재장전 중 여부
    private bool isReloading = false;
    // 마지막 발사 시각 (Time.time 기준)
    private float lastFireTime = -999f;

    // 재장전 중 숨길 MeshRenderer 목록
    private MeshRenderer[] gunMeshRenderers;

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

        bool fireHeld = ARAVRInput.Get(ARAVRInput.Button.IndexTrigger)
                        || Input.GetKey(KeyCode.LeftShift)
                        || Input.GetKey(KeyCode.RightShift);

        if (fireHeld)
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

        ARAVRInput.PlayVibration(0.04f, 1f, 0.5f, ARAVRInput.Controller.RTouch);

        // 발사 사운드 재생 - PlayOneShot으로 발사마다 겹쳐서 재생해 연사음 끊김 방지
        if (fireAudio != null && fireClip != null)
        {
            fireAudio.PlayOneShot(fireClip);
        }

        Vector3 baseDir   = ARAVRInput.RHandDirection.normalized;
        Vector3 spreadDir = ApplySpread(baseDir);

        Ray ray = new Ray(ARAVRInput.RHandPosition, spreadDir);
        RaycastHit hitInfo;

        int playerLayer = 1 << LayerMask.NameToLayer("Player");
        int towerLayer  = 1 << LayerMask.NameToLayer("Tower");
        int layerMask   = playerLayer | towerLayer;

        if (Physics.Raycast(ray, out hitInfo, 200, ~layerMask))
        {
            bulletEffect.Stop();
            bulletEffect.Play();
            bulletImpact.position = hitInfo.point;
            bulletImpact.forward  = hitInfo.normal;

            DroneAI drone = hitInfo.transform.GetComponent<DroneAI>()
                         ?? hitInfo.transform.GetComponentInParent<DroneAI>();
            if (drone != null)
            {
                drone.OnDamageProcess(damage);
            }
        }

        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
        }
    }

    private Vector3 ApplySpread(Vector3 baseDir)
    {
        float randomX = Random.Range(-spreadAngle, spreadAngle);
        float randomY = Random.Range(-spreadAngle, spreadAngle);

        Quaternion spreadRotation = Quaternion.Euler(randomX, randomY, 0);
        return spreadRotation * baseDir;
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
        Debug.Log("[MachineGun] 재장전 중... (" + reloadTime + "초)");

        SetGunVisible(false);

        // 발사 사운드 중지 후 재장전 사운드 재생
        if (fireAudio != null) fireAudio.Stop();
        if (reloadAudio != null && reloadClip != null)
        {
            reloadAudio.Stop();
            reloadAudio.Play();
        }

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;

        SetGunVisible(true);

        Debug.Log("[MachineGun] 재장전 완료! 탄약: " + currentAmmo + " / " + maxAmmo);
    }

    public int CurrentAmmo  => currentAmmo;
    public int MaxAmmo      => maxAmmo;
    public bool IsReloading => isReloading;
    public bool IsCoolingDown => !isReloading && (Time.time - lastFireTime < fireRate);

    public void OnWeaponDeactivate()
    {
        StopAllCoroutines();
        isReloading = false;

        // 무기 비활성화 시 모든 사운드 중단
        if (fireAudio != null)   fireAudio.Stop();
        if (reloadAudio != null) reloadAudio.Stop();

        SetGunVisible(true);
    }
}