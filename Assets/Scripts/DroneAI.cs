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
    public float moveSpeed = 1f;
    // 타워 위치
    private Transform tower;
    // 길 찾기 수행 내비게이션 메시 에이전트
    private NavMeshAgent agent;
    // 공격 범위
    public float attackRange = 3f;
    // 공격 지연 시간
    public float attackDelayTime = 2f;

    [SerializeField] int hp = 3;



    void Start()
    {
        // 타워 찾기
        // tower = GameObject.Find("Tower").transform;
        tower = FindObjectOfType<Tower>().gameObject.transform;

        // NavMeshAgent 컴포넌트 가져오기
        agent = GetComponent<NavMeshAgent>();
        // agent 속도 설정
        agent.speed = moveSpeed;
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
                Damage();
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

    private void Damage()
    {

    }

    private void Die()
    {

    }

    public void OnDamageProcess()
    {
        // 체력을 감소시키고 죽지 않았다면 상태를 데미지로 전환하고 싶다
    }
}
