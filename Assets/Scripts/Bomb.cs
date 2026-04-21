using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    private Transform explosion;
    private ParticleSystem expEffect;
    private AudioSource expAudio;

    // 폭발 범위
    public float range = 5f;

    // 보스 드론에게 주는 데미지
    private int bossDamage = 20;

    void Start()
    {
        explosion = GameObject.Find("Explosion").transform;
        expEffect = explosion.GetComponent<ParticleSystem>();
        expAudio  = explosion.GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        int layerMask = 1 << LayerMask.NameToLayer("Drone");
        Collider[] drones = Physics.OverlapSphere(transform.position, range, layerMask);

        foreach (Collider col in drones)
        {
            DroneAI drone = col.GetComponent<DroneAI>();

            if (drone != null)
            {
                if (drone.isBoss)
                {
                    // 보스 드론 → 20 데미지
                    drone.OnDamageProcess(bossDamage);
                }
                else
                {
                    // 일반 드론 → 즉사
                    drone.OnDamageProcess(int.MaxValue);
                }
            }
            else
            {
                // DroneAI가 없는 경우 기존 방식대로 제거
                Destroy(col.gameObject);
            }
        }

        explosion.position = transform.position;
        expEffect.Stop();
        expEffect.Play();
        expAudio.Stop();
        expAudio.Play();
        Destroy(gameObject);
    }
}