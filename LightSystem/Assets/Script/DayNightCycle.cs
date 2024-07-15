using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public float cycleLenght = 240f;        // 하루의 길이(초)
    public Light directionalLight;          // 다이랙션얼 라이트 할당

    void Update()
    {
        float cycleCompletionPerectage = (Time.time % cycleLenght) / cycleLenght;       // 사이클 % 설정
        float sunAngle = cycleCompletionPerectage * 360.0f;                             // 사이클 %에 따른 각도 계산

        directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0);      // 계산된 각도로 라이트를 돌린다.
    }
}
