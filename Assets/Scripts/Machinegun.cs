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
    private int damage = 2;
    // 현재 탄창 / 최대 탄창
    private int currentAmmo;
    private int maxAmmo = 25;
    // 재장전 시간 (초)
    private float reloadTime = 2.5f;
    // 공격 속도: 발사 후 다음 발사까지의 최소 간격 (초)
    private float fireRate = 0.15f;

    // 탄퍼짐 최대 각도 (도)
    private float spreadAngle = 4f;

    // 재장전 중 여부
    private bool isReloading = false;
    // 마지막 발사 시각 (Time.time 기준)
    private float lastFireTime = -999f;

    // 재장전 중 숨길 MeshRenderer 목록
    private MeshRenderer[] gunMeshRenderers;

    void Start()
    {
        bulletEffect = bulletImpact.GetComponent<ParticleSystem>();

        AudioSource[] sources = GetComponents<AudioSource>();

        fireAudio = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        fireAudio.playOnAwake = false;
        fireAudio.clip = fireClip;

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

        ARAVRInput.PlayVibration(ARAVRInput.Controller.RTouch);

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

            if (hitInfo.transform.name.Contains("Drone"))
            {
                DroneAI drone = hitInfo.transform.GetComponent<DroneAI>();
                if (drone)
                {
                    drone.OnDamageProcess(damage);
                }
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

    public void OnWeaponDeactivate()
    {
        StopAllCoroutines();
        isReloading = false;

        if (fireAudio != null)   fireAudio.Stop();
        if (reloadAudio != null) reloadAudio.Stop();

        SetGunVisible(true);
    }
}