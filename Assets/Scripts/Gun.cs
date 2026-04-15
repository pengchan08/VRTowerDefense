using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public Transform bulletImpact;
    private ParticleSystem bulletEffect;

    [Header("Sound")]
    public AudioClip fireClip;
    public AudioClip reloadClip;

    private AudioSource fireAudio;
    private AudioSource reloadAudio;

    // Crosshair를 위한 속성
    public Transform crosshair;

    // 1발당 피해량
    private int damage = 5;
    // 현재 탄창 / 최대 탄창
    private int currentAmmo;
    private int maxAmmo = 6;
    // 재장전 시간 (초)
    private float reloadTime = 1.2f;
    // 공격 속도: 발사 후 다음 발사까지의 최소 간격 (초)
    private float fireRate = 1.0f;

    // 재장전 중 여부
    private bool isReloading = false;
    private float lastFireTime = -999f;

    private MeshRenderer[] gunMeshRenderers;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public bool IsReloading => isReloading;

    void Start()
    {
        // 총알 효과 파티클 시스템 컴포넌트 가져오기
        bulletEffect = bulletImpact.GetComponent<ParticleSystem>();

        AudioSource[] sources = GetComponents<AudioSource>();

        fireAudio = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        fireAudio.playOnAwake = false;
        fireAudio.clip = fireClip;

        reloadAudio = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        reloadAudio.playOnAwake = false;
        reloadAudio.clip = reloadClip;

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

        ARAVRInput.PlayVibration(ARAVRInput.Controller.RTouch);

        if (fireAudio != null && fireClip != null)
        {
            fireAudio.Stop();
            fireAudio.Play();
        }

        Ray ray = new Ray(ARAVRInput.RHandPosition, ARAVRInput.RHandDirection);
        RaycastHit hitInfo;

        int playerLayer = 1 << LayerMask.NameToLayer("Player");
        int towerLayer = 1 << LayerMask.NameToLayer("Tower");
        int layerMask = playerLayer | towerLayer;

        if (Physics.Raycast(ray, out hitInfo, 200, ~layerMask))
        {
            bulletEffect.Stop();
            bulletEffect.Play();
            bulletImpact.position = hitInfo.point;
            bulletImpact.forward = hitInfo.normal;

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
        Debug.Log("[Gun] 재장전 중... (" + reloadTime + "초)");

        SetGunVisible(false);

        if (reloadAudio != null && reloadClip != null)
        {
            reloadAudio.Stop();
            reloadAudio.Play();
        }

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;

        SetGunVisible(true);

        Debug.Log("[Gun] 재장전 완료! 탄약: " + currentAmmo + " / " + maxAmmo);
    }

    public void OnWeaponDeactivate()
    {
        StopAllCoroutines();
        isReloading = false;

        if (reloadAudio != null) reloadAudio.Stop();

        SetGunVisible(true);
    }
}