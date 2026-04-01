using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tower : MonoBehaviour
{
    public static Tower Instance = null;

    // 데미지 표현할 UI
    public Transform damageUI;
    public Image damageImage;

    // 타워의 최초 HP
    public int initialHP = 10;
    private int _hp = 0;
    public float damageTime = 0.1f;

    public int HP
    {
        get
        {
            return _hp;
        }
        set
        {
            _hp = value;
            StopAllCoroutines();
            StartCoroutine(DamageEvent());

            // hp가 0이하이면 제거
            if (_hp <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        _hp = initialHP;
        // 카메라의 nearClipPlane값을 기억해 둔다
        float z = Camera.main.nearClipPlane + 0.01f;
        // damageUI 객체의 부모를 카메라로 설정
        damageUI.parent = Camera.main.transform;
        // damageUI의 위치를 X, Y는 0, Z 값은 카메라의 near 값으로 설정
        damageUI.localPosition = new Vector3(0, 0, z);
        // damageImage는 보이지 않게  초기에 비활성화 해 놓는다
        damageImage.enabled = false;
    }

    void Update()
    {

    }

    IEnumerator DamageEvent()
    {
        damageImage.enabled = true;
        yield return new WaitForSeconds(damageTime);
        damageImage.enabled = false;
    }
}
