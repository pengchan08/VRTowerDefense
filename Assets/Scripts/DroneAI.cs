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
    public float normalSpeed       = 3f;
    public float normalAttackRange = 3f;
    public float normalAttackRate  = 2.0f;
    public int   normalDamage      = 1;

    [Header("Boss Drone Stats")]
    public int   bossHP          = 100;
    public float bossSpeed       = 1.5f;
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
    public ParticleSystem hitEffect;        // 피격 이펙트
    public GameObject     deathEffectPrefab; // 사망 이펙트 프리팹

    public int   HP     => _hp;
    public float Speed  => _speed;
    public int   Damage => _damage;

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
            agent.speed            = _speed;
            agent.stoppingDistance = _attackRange;
        }
    }

    void Update()
    {
        if (target == null || agent == null) return;

        float distToTarget = Vector3.Distance(transform.position, target.position);

        if (!isAttacking)
        {
            agent.SetDestination(target.position);

            if (distToTarget <= _attackRange)
            {
                isAttacking     = true;
                agent.isStopped = true;
            }
        }
        else
        {
            if (Time.time - lastAttackTime >= _attackRate)
            {
                lastAttackTime = Time.time;
                AttackTower();
            }
        }
    }

    private void AttackTower()
    {
        if (Tower.Instance == null) return;

        Tower.Instance.HP -= _damage;

        if (Tower.Instance == null)
        {
            Destroy(gameObject);
        }
    }

    public void OnDamageProcess(int damage)
    {
        _hp -= damage;

        // 피격 이펙트 재생
        if (hitEffect != null)
        {
            hitEffect.Stop();
            hitEffect.Play();
        }

        if (_hp <= 0)
        {
            // 사망 이펙트 생성 후 드론 제거
            if (deathEffectPrefab != null)
            {
                GameObject effect = Instantiate(deathEffectPrefab, transform.position, transform.rotation);
                // 파티클 재생 시간 후 자동 제거
                ParticleSystem ps = effect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
                }
                else
                {
                    Destroy(effect, 2f);
                }
            }

            Destroy(gameObject);
        }
    }
}