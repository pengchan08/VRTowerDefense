using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DroneAI : MonoBehaviour
{
    [Header("Boss Settings")]
    public bool isBoss = false;

    [Header("Normal Drone Stats")]
    public int   normalHP          = 20;
    public float normalSpeed       = 1.2f;
    public float normalAttackRange = 3f;
    public float normalAttackRate  = 2.0f;
    public int   normalDamage      = 1;

    [Header("Boss Drone Stats")]
    public int   bossHP          = 250;
    public float bossSpeed       = 1f;
    public float bossAttackRange = 4f;
    public float bossAttackRate  = 2.5f;
    public int   bossDamage      = 2;

    private int   _hp;
    private float _speed;
    private float _attackRange;
    private float _attackRate;
    private int   _damage;

    [Header("Movement")]
    public Transform target;
    private NavMeshAgent agent;

    private bool  isAttacking    = false;
    private float lastAttackTime = -999f;

    [Header("Effects")]
    public ParticleSystem hitEffect;
    public GameObject     deathEffectPrefab;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (isBoss)
        {
            _hp          = bossHP;
            _speed       = bossSpeed;
            _attackRange = bossAttackRange;
            _attackRate  = bossAttackRate;
            _damage      = bossDamage;

            transform.localScale *= 2f;
        }
        else
        {
            _hp          = normalHP;
            _speed       = normalSpeed;
            _attackRange = normalAttackRange;
            _attackRate  = normalAttackRate;
            _damage      = normalDamage;
        }

        if (agent != null)
        {
            agent.speed = _speed;
            // stoppingDistance는 설정하지 않음
            // 보스 크기 2배를 고려해 distToTarget으로 직접 판정
            agent.stoppingDistance = 0f;
        }
    }

    void Update()
    {
        if (target == null || agent == null) return;

        float distToTarget = Vector3.Distance(transform.position, target.position);

        if (!isAttacking)
        {
            // 공격 범위 밖 → 이동
            if (distToTarget > _attackRange)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
            else
            {
                // 공격 범위 안 → 즉시 공격 시작
                agent.isStopped = true;
                isAttacking = true;
            }
        }
        else
        {
            // 공격 범위를 벗어났다면 다시 추적 (타워 HP 변화 등으로 위치가 바뀔 경우 대비)
            if (distToTarget > _attackRange)
            {
                isAttacking     = false;
                agent.isStopped = false;
                return;
            }

            // 공격 간격마다 데미지
            if (Time.time - lastAttackTime >= _attackRate)
            {
                lastAttackTime = Time.time;
                AttackTower();
            }
        }
    }

    private void AttackTower()
    {
        if (Tower.Instance == null)
        {
            Destroy(gameObject);
            return;
        }

        Tower.Instance.HP -= _damage;

        // HP를 깎은 뒤 타워가 파괴됐는지는 다음 프레임에 Tower.Instance로 확인
        // Tower.cs에서 HP <= 0이면 Destroy(gameObject)를 호출하므로
        // 타워 파괴 후 드론은 target이 null이 되어 자동으로 Update 진입 차단됨
    }

    public void OnDamageProcess(int damage)
    {
        _hp -= damage;

        if (hitEffect != null)
        {
            hitEffect.Stop();
            hitEffect.Play();
        }

        if (_hp <= 0)
        {
            if (deathEffectPrefab != null)
            {
                GameObject effect = Instantiate(deathEffectPrefab, transform.position, transform.rotation);
                ParticleSystem ps = effect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop();
                    ps.Play();
                    Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
                }
                else
                    Destroy(effect, 2f);
            }

            Destroy(gameObject);
        }
    }

    public int   HP     => _hp;
    public float Speed  => _speed;
    public int   Damage => _damage;
}