using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public Transform bulletImpact;
    private ParticleSystem bulletEffect;
    private AudioSource bulletAudio;
    // Crosshair를 위한 속성
    public Transform crosshair;

    // 1발당 피해량
    private int damage = 5;
    // 현재 탄창 / 최대 탄창
    private int currentAmmo;
    private int maxAmmo = 6;
    // 재장전 시간 (초)
    private float reloadTime = 1f;
    // 공격 속도: 발사 후 다음 발사까지의 최소 간격 (초)
    private float fireRate = 1f;

    // 재장전 중 여부
    private bool isReloading = false;
    // 마지막 발사 시각 (Time.time 기준)
    private float lastFireTime = -999f;

    public GameObject gunVisual;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public bool IsReloading => isReloading;

    void Start()
    {
        // 총알 효과 파티클 시스템 컴포넌트 가져오기
        bulletEffect = bulletImpact.GetComponent<ParticleSystem>();
        // 총알 효과 오디오 소스 컴포넌트 가져오기
        bulletAudio = bulletImpact.GetComponent<AudioSource>();

        // 탄창 초기화
        currentAmmo = maxAmmo;
    }

    void Update()
    {
        // 크로스헤어 표시
        ARAVRInput.DrawCrosshair(crosshair);

        // 재장전 중이면 발사 불가
        if (isReloading) return;

        bool fireInput = ARAVRInput.GetDown(ARAVRInput.Button.IndexTrigger)
                         || Input.GetKeyDown(KeyCode.LeftShift)
                         || Input.GetKeyDown(KeyCode.RightShift);

        if (fireInput)
        {
            // 탄창이 비어있으면 재장전 시작
            if (currentAmmo <= 0)
            {
                StartCoroutine(Reload());
                return;
            }

            // 공격 속도(발사 간격) 체크
            if (Time.time - lastFireTime < fireRate) return;

            // 발사 처리
            Fire();
        }
    }

    private void Fire()
    {
        // 마지막 발사 시각 갱신
        lastFireTime = Time.time;
        // 탄약 소모
        currentAmmo--;

        // VR 컨트롤러 진동 재생
        ARAVRInput.PlayVibration(ARAVRInput.Controller.RTouch);

        // 총알 오디오 재생
        bulletAudio.Stop();
        bulletAudio.Play();

        // Ray 생성 (VR 오른손 방향 기준)
        Ray ray = new Ray(ARAVRInput.RHandPosition, ARAVRInput.RHandDirection);
        RaycastHit hitInfo;

        // 플레이어 레이어 얻어오기
        int playerLayer = 1 << LayerMask.NameToLayer("Player");
        // 타워 레이어 얻어오기
        int towerLayer = 1 << LayerMask.NameToLayer("Tower");
        int layerMask = playerLayer | towerLayer;

        // Ray를 쏜다 ray가 부딪힌 정보는 hitInfo에 담긴다
        if (Physics.Raycast(ray, out hitInfo, 200, ~layerMask))
        {
            // 총알 파편 효과 처리
            bulletEffect.Stop();
            bulletEffect.Play();
            // 부딪힌 지점 바로 위에서 이펙트가 보이도록 설정
            bulletImpact.position = hitInfo.point;
            // 부딪힌 지점 방향으로 총알 이펙트의 방향을 설정
            bulletImpact.forward = hitInfo.normal;

            // ray와 부딪힌 객체가 drone이면 피격 처리
            if (hitInfo.transform.name.Contains("Drone"))
            {
                DroneAI drone = hitInfo.transform.GetComponent<DroneAI>();
                if (drone)
                {
                    // 데미지 값을 직접 전달
                    drone.OnDamageProcess(damage);
                }
            }
        }

        // 탄창이 비면 자동 재장전
        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("[Gun] 재장전 중... (" + reloadTime + "초)");

        // 재장전 중에는 총기 비주얼 비활성화
        if (gunVisual != null) gunVisual.SetActive(false);

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;

        // 재장전 완료 후 총기 비주얼 다시 활성화
        if (gunVisual != null) gunVisual.SetActive(true);

        Debug.Log("[Gun] 재장전 완료! 탄약: " + currentAmmo + " / " + maxAmmo);
    }

    public void OnWeaponDeactivate()
    {
        StopAllCoroutines();
        isReloading = false;
        if (gunVisual != null) gunVisual.SetActive(true);
    }
}