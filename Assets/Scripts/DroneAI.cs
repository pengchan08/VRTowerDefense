using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneAI : MonoBehaviour
{
    // 보스 여부
    [Header("Boss Settings")]
    public bool isBoss = false;

    // 일반 드론 스탯
    [Header("Normal Drone Stats")]
    public int   normalHP     = 10;
    public float normalSpeed  = 5f;
    public int   normalDamage = 1;

    // 보스 드론 스탯
    [Header("Boss Drone Stats")]
    public int   bossHP     = 50;
    public float bossSpeed  = 3f;
    public int   bossDamage = 2;

    // 런타임 스탯
    private int   _hp;
    private float _speed;
    private int   _damage;

    // 이동 목표
    [Header("Movement")]
    public Transform target;

    // 피격 이펙트
    [Header("Hit Effect")]
    public ParticleSystem hitEffect;

    public int   HP     => _hp;
    public float Speed  => _speed;
    public int   Damage => _damage;

    void Start()
    {
        if (isBoss)
        {
            _hp     = bossHP;
            _speed  = bossSpeed;
            _damage = bossDamage;

            // 보스는 크기 2배
            transform.localScale *= 2f;
        }
        else
        {
            _hp     = normalHP;
            _speed  = normalSpeed;
            _damage = normalDamage;
        }
    }

    void Update()
    {
        if (target == null) return;

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * _speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) < 1f)
        {
            ReachTarget();
        }
    }

    private void ReachTarget()
    {
        if (Tower.Instance != null)
        {
            Tower.Instance.HP -= _damage;
        }
        Destroy(gameObject);
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
            Destroy(gameObject);
        }
    }
}