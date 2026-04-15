using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun : MonoBehaviour
{
    public Transform bulletImpact;
    private ParticleSystem bulletEffect;

    [Header("Sound")]
    public AudioClip fireClip;      // 발사 사운드 클립
    public AudioClip reloadClip;    // 재장전 사운드 클립

    private AudioSource fireAudio;   // 발사 전용 AudioSource
    private AudioSource reloadAudio; // 재장전 전용 AudioSource

    // Crosshair를 위한 속성
    public Transform crosshair;

    // 펠릿 1발당 피해량
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

    // 펠릿 퍼짐 각도 (도) — 값이 클수록 더 넓게 퍼짐
    private float spreadAngle = 8f;

    private Vector3[] pelletDirections = new Vector3[]
    {
        new Vector3( 0.00f,  0.00f, 1f),
        new Vector3( 0.15f,  0.00f, 1f),
        new Vector3(-0.15f,  0.00f, 1f),
        new Vector3( 0.00f,  0.15f, 1f),
        new Vector3( 0.00f, -0.15f, 1f),
    };

    private bool isReloading = false;
    private float lastFireTime = -999f;

    private MeshRenderer[] gunMeshRenderers;

    public int CurrentAmmo  => currentAmmo;
    public int MaxAmmo      => maxAmmo;
    public bool IsReloading => isReloading;

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

        int playerLayer = 1 << LayerMask.NameToLayer("Player");
        int towerLayer  = 1 << LayerMask.NameToLayer("Tower");
        int layerMask   = playerLayer | towerLayer;

        bool hitSomething = false;

        foreach (Vector3 localDir in pelletDirections)
        {
            Vector3 worldDir = TransformPelletDirection(localDir);
            Ray ray = new Ray(ARAVRInput.RHandPosition, worldDir);
            RaycastHit hitInfo;

            if (Physics.Raycast(ray, out hitInfo, 200, ~layerMask))
            {
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

    private Vector3 TransformPelletDirection(Vector3 localDir)
    {
        Vector3 forward = ARAVRInput.RHandDirection.normalized;
        Vector3 right   = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 up      = Vector3.Cross(forward, right).normalized;

        Vector3 baseDir = (forward * localDir.z
                         + right   * localDir.x
                         + up      * localDir.y).normalized;

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

        if (reloadAudio != null) reloadAudio.Stop();

        SetGunVisible(true);
    }
}