using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunManager : MonoBehaviour
{
    public GameObject[] guns;

    private int currentIndex = 0;

    public int CurrentIndex => currentIndex;

    void Start()
    {
        // 시작 시 첫 번째 무기만 활성화, 나머지 비활성화
        for (int i = 0; i < guns.Length; i++)
        {
            guns[i].SetActive(i == currentIndex);
        }
    }

    void Update()
    {
        bool swapInput = ARAVRInput.GetDown(ARAVRInput.Button.Two, ARAVRInput.Controller.RTouch)
                         || Input.GetKeyDown(KeyCode.R);

        if (swapInput)
        {
            SwapWeapon();
        }
    }

    private void SwapWeapon()
    {
        // 현재 무기 비활성화 전에 코루틴 정리
        Gun currentGun = guns[currentIndex].GetComponent<Gun>();
        if (currentGun != null)
        {
            currentGun.OnWeaponDeactivate();
        }
        guns[currentIndex].SetActive(false);

        // 다음 인덱스로 순환 (0 → 1 → 2 → 0 ...)
        currentIndex = (currentIndex + 1) % guns.Length;

        // 다음 무기 활성화
        guns[currentIndex].SetActive(true);

        Debug.Log("[GunManager] 무기 전환 → " + guns[currentIndex].name);
    }
}