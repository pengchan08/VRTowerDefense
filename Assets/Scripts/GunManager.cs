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
            if (IsCurrentGunReloading())
            {
                Debug.Log("[GunManager] 재장전 중에는 무기를 전환할 수 없습니다.");
                return;
            }

            SwapWeapon();
        }
    }

    private bool IsCurrentGunReloading()
    {
        GameObject currentGun = guns[currentIndex];

        Gun gun = currentGun.GetComponent<Gun>();
        if (gun != null) return gun.IsReloading;

        Shotgun shotgun = currentGun.GetComponent<Shotgun>();
        if (shotgun != null) return shotgun.IsReloading;

        MachineGun machineGun = currentGun.GetComponent<MachineGun>();
        if (machineGun != null) return machineGun.IsReloading;

        return false;
    }

    private void SwapWeapon()
    {
        NotifyDeactivate(guns[currentIndex]);
        guns[currentIndex].SetActive(false);

        currentIndex = (currentIndex + 1) % guns.Length;

        guns[currentIndex].SetActive(true);
        Debug.Log("[GunManager] 무기 전환 → " + guns[currentIndex].name);
    }

    private void NotifyDeactivate(GameObject gunObj)
    {
        Gun gun = gunObj.GetComponent<Gun>();
        if (gun != null) { gun.OnWeaponDeactivate(); return; }

        Shotgun shotgun = gunObj.GetComponent<Shotgun>();
        if (shotgun != null) { shotgun.OnWeaponDeactivate(); return; }

        MachineGun machineGun = gunObj.GetComponent<MachineGun>();
        if (machineGun != null) { machineGun.OnWeaponDeactivate(); return; }
    }
}