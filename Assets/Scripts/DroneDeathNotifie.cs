using System;
using UnityEngine;

// 드론이 파괴될 때 WaveManager에 사망을 통보하는 헬퍼 컴포넌트
public class DroneDeathNotifier : MonoBehaviour
{
    public Action onDeath;

    void OnDestroy()
    {
        onDeath?.Invoke();
    }
}