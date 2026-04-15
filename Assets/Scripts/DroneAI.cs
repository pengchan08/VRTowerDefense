using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DroneAI : MonoBehaviour
{
    // 드론의 상태 상수 정의
    enum DroneState
    {
        Idle,
        Move,
        Attack,
        Damage,
        Die,
    }

    // 초기 시작 상태는 Idle로 설정
    DroneState state = DroneState.Idle;
    // 대기 상태의 지속시간
    public float idleDelayTime = 2f;
    // 경과 시간
    private float currentTime;

    // 이동 속도
    public float moveSpeed = 5f;
    // 타워 위치
    private Transform tower;
    // 길 찾기 수행 내비게이션 메시 에이전트
    private NavMeshAgent agent;
    // 공격 범위
    public float attackRange = 3f;
    // 공격 지연 시간
    public float attackDelayTime = 2f;

    [SerializeField] int hp = 20;


    // 폭발 효과
    private Transform explosion;
    private ParticleSystem expEffect;
    private AudioSource expAudio;

    void Start()
    {
        // 타워 찾기
        // tower = GameObject.Find("Tower").transform;
        tower = FindObjectOfType<Tower>().gameObject.transform;

        // NavMeshAgent 컴포넌트 가져오기
        agent = GetComponent<NavMeshAgent>();
        agent.enabled = false;
        // agent 속도 설정
        agent.speed = moveSpeed;

        explosion = GameObject.Find("Explosion").transform;
        expEffect = explosion.GetComponent<ParticleSystem>();
        expAudio = explosion.GetComponent<AudioSource>();
    }

    void Update()
    {
        print("current state: " + state);

        switch (state)
        {
            case DroneState.Idle:
                Idle();
                break;
            case DroneState.Move:
                Move();
                break;
            case DroneState.Attack:
                Attack();
                break;
            case DroneState.Damage:
                // Damage();
                break;
            case DroneState.Die:
                Die();
                break;
        }
    }

    private void Idle()
    {
        // 시간 경과
        currentTime += Time.deltaTime;
        if (currentTime > idleDelayTime)
        {
            // 상태 전환
            state = DroneState.Move;
            // agent 활성화
            agent.enabled = true;
        }
    }

    private void Move()
    {
        // 내비게이션 할 목적지 설정
        agent.SetDestination(tower.position);
        // 공격 범위 안에 들어오면 공격 상태로 전환
        if (Vector3.Distance(transform.position, tower.position) < attackRange)
        {
            state = DroneState.Attack;
            agent.enabled = false;
        }
    }

    private void Attack()
    {
        currentTime += Time.deltaTime;
        if (currentTime > attackDelayTime)
        {
            // 공격
            Tower.Instance.HP--;
            // 경과시간 초기화
            currentTime = 0f;
        }
    }

    IEnumerator Damage()
    {
        // 1. 길 찾기 중지
        agent.enabled = false;
        // 2. 자식 객체의 MeshRenderer에서 재질 받아오기
        Material mat = GetComponentInChildren<MeshRenderer>().material;
        // 3. 원래 색을 지정
        Color originalColor = mat.color;
        // 4. 재질의 색 변경
        mat.color = Color.red;
        // 5. 0.1초 기다리기
        yield return new WaitForSeconds(0.1f);
        // 6. 재질의 색을 원래대로
        mat.color = originalColor;
        // 7. 상태를 Idle로 전환
        state = DroneState.Idle;
        // 8. 경과 시간 초기화
        currentTime = 0;
    }

    private void Die()
    {

    }

    // 기존 호환성 유지: 외부에서 damage 값 없이 호출할 경우 기본 1 데미지
    public void OnDamageProcess()
    {
        OnDamageProcess(1);
    }

    // 데미지 값을 직접 받는 오버로드 함수
    public void OnDamageProcess(int damage)
    {
        // 데미지만큼 체력 감소
        hp -= damage;
        if (hp > 0)
        {
            // 상태를 데미지로 전환
            state = DroneState.Damage;
            // 코루틴 호출
            StopAllCoroutines();
            StartCoroutine(Damage());
        }
        else
        {
            explosion.position = transform.position;
            expEffect.Play();
            expAudio.Play();
            Destroy(gameObject);
        }
    }
}