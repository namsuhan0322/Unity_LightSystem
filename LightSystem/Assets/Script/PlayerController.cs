using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // 라이트에 의한 데미지 설정 클래스
    [System.Serializable]
    public class LightDamageSettings                    // 일반 빛에 데미지
    {
        public float nearDistance = 2f;                 // 가까운 거리 기준
        public float mediumDistance = 5f;               // 중간 거리 기준
        public int nearDamage = 3;                      // 가까운 거리에서의 데미지
        public int mediumDamage = 2;                    // 중간 거리에서의 데미지
        public int farDamage = 1;                       // 먼 거리에서의 데미지
    }

    [System.Serializable]
    public class directionalLightSettings
    {
        public int baseDamage = 1;                      // 기본 데미지
        public int maxDamage = 5;                       // 최대 데미지
        public float damagelIncreaseInterval = 2f;      // 데미지 증가 간격
    }

    public LightDamageSettings lightDamage;
    public directionalLightSettings directionalLightDamage;

    public float damagedInterval = 1.0f;                // 데미지를 받는 간견
    public float nightThreshold = 0.2f;                 // 밤으로 간주할 빛의 강도 임계값
    public float moveSpeed = 5f;                        // 이동속도

    private CharacterController controller;             // 캐릭터 컨트롤러 컴포넌트
    private Light[] sceneLights;                         // 씬의 모든 라이트를 가져와서 배열에 넣는다.
    private int currentDirectionalLightDamage;          // 현재 디렉셔녈 라이트 데미지
    private float lastDamageTime;                       // 마지막으로 데미지를 받은 시간
    private float lastDirectionalLightDamageTIme;         // 마지막으로 디렉셔녈 라이트 데미지가 증가한 시간
    private float cumalativeDamage;                     // 누적 데미지

    void Start()        // 데이터 초기화
    {
        controller = GetComponent<CharacterController>();
        sceneLights = FindObjectsOfType<Light>();       // Scene의 모든 라이트 찾기
        ResetDirectionalLightDamage();
    }

    void Update()
    {
        // 플레이어 이동처리
        Vector3 movement = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        controller.Move(movement * moveSpeed * Time.deltaTime);

        // 데미지 처리
        if (IsExposedToLight())
        {
            if (Time.time - lastDamageTime >= damagedInterval)
            {
                TakeDamage();
            }
            UpdateDirectionalLightDamage();
        }
        else
        {
            ResetDirectionalLightDamage();
        }
    }

    void TakeDamage()               // 데미지 적용
    {
        int damage = CalculateDamage();
        cumalativeDamage += damage;
        lastDamageTime = Time.time;
        Debug.Log($"플레이어가 데미지를 받음 : {damage} , 누적 데미지 : {cumalativeDamage}");
    }

    int CalculateDamage()
    {
        int damage = currentDirectionalLightDamage;

        float closestPointLightDistance = float.MaxValue;
        bool exposedToPointLight = false;

        // 가장 가까운 포인트 라이트 찾기
        for (int i = 0; i < sceneLights.Length; i++)
        {
            if (sceneLights[i].type == LightType.Point && isExposedToPointLight(sceneLights[i]))
            {
                float distance = Vector3.Distance(transform.position, sceneLights[i].transform.position);
                if (distance < closestPointLightDistance)
                {
                    closestPointLightDistance = distance;
                    exposedToPointLight = true;
                }
            }
        }

        // 포인트 라이트에 거리에 의한 데미지 계산
        if (exposedToPointLight)
        {
            if (closestPointLightDistance <= lightDamage.nearDistance) damage += (int)lightDamage.nearDistance;
            else if (closestPointLightDistance <= lightDamage.mediumDistance) damage += (int)lightDamage.mediumDistance;
            else damage += (int)lightDamage.farDamage;
        }

        return damage;
    }

    // 플레이어가 빛에 노출되어있는지 확인
    bool IsExposedToLight()
    {
        return IsExposedToDirectionalLight() || IsExposedToAnyPointLIght();               // 라이트 타입이 추가되면 추가 코딩 해준다.
    }

    // 포인트 라이트에 노출 되어있는지 확인
    bool isExposedToPointLight(Light light)
    {
        Vector3 directionalToLight = light.transform.position - transform.position;         // 받아온 포인트의 방향값을 구한다.
        return directionalToLight.magnitude <= light.range && 
            !Physics.Raycast(transform.position, directionalToLight.normalized, directionalToLight.magnitude);
    }

    bool IsExposedToAnyPointLIght()
    {
        for (int i = 0; i < sceneLights.Length; i++)
        {
            if (sceneLights[i].type == LightType.Point && isExposedToPointLight(sceneLights[i]))
            {
                return true;
            }
        }

        return false;
    }

    bool IsExposedToDirectionalLight()
    {
        for (int i = 0; i < sceneLights.Length; i++)
        {
            if (sceneLights[i].type == LightType.Directional && !isInDirectionalShadow(sceneLights[i]))
            {
                return true;
            }
        }

        return false;
    }

    bool isInDirectionalShadow(Light light)
    {
        const int rayCount = 5;
        const float rayRadius = 0.5f;
        int shadowCount = 0;

        for (int i = 0; i < rayCount; i++)
        {
            Vector3 rayStart = transform.position + Quaternion.Euler(0, i * 360.0f / rayCount, 0) * (Vector3.forward * rayRadius);
            if (Physics.Raycast(rayStart, -light.transform.forward, out _))
            {
                shadowCount++;
            }
        }

        return shadowCount > rayCount / 2;
    }

    void UpdateDirectionalLightDamage()
    {
        if (Time.time - lastDirectionalLightDamageTIme >= directionalLightDamage.damagelIncreaseInterval)
        {
            currentDirectionalLightDamage = Mathf.Min(currentDirectionalLightDamage + 1, directionalLightDamage.maxDamage);
            lastDirectionalLightDamageTIme = Time.time;
        }
    }

    // 디렉셔널 라이트 데미지 리셋
    void ResetDirectionalLightDamage()
    {
        currentDirectionalLightDamage = directionalLightDamage.baseDamage;
    }
}
