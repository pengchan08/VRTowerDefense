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

    void Start()
    {
        explosion = GameObject.Find("Explosion").transform;
        expEffect = explosion.GetComponent<ParticleSystem>();
        expAudio = explosion.GetComponent<AudioSource>();
    }

    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 레이어 마스크 가져오기
        int layerMask = 1 << LayerMask.NameToLayer("Drone");
        Collider[] drons = Physics.OverlapSphere(transform.position, range, layerMask);
        foreach(Collider drone in drons)
        {
            Destroy(drone.gameObject);
        }

        explosion.position = transform.position;
        expEffect.Play();
        expAudio.Play();
        Destroy(gameObject);
    }
}
