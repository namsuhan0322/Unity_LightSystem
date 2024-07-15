using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // ����Ʈ�� ���� ������ ���� Ŭ����
    [System.Serializable]
    public class LightDamageSettings                    // �Ϲ� ���� ������
    {
        public float nearDistance = 2f;                 // ����� �Ÿ� ����
        public float mediumDistance = 5f;               // �߰� �Ÿ� ����
        public int nearDamage = 3;                      // ����� �Ÿ������� ������
        public int mediumDamage = 2;                    // �߰� �Ÿ������� ������
        public int farDamage = 1;                       // �� �Ÿ������� ������
    }

    [System.Serializable]
    public class directionalLightSettings
    {
        public int baseDamage = 1;                      // �⺻ ������
        public int maxDamage = 5;                       // �ִ� ������
        public float damagelIncreaseInterval = 2f;      // ������ ���� ����
    }

    public LightDamageSettings lightDamage;
    public directionalLightSettings directionalLightDamage;

    public float damagedInterval = 1.0f;                // �������� �޴� ����
    public float nightThreshold = 0.2f;                 // ������ ������ ���� ���� �Ӱ谪
    public float moveSpeed = 5f;                        // �̵��ӵ�

    private CharacterController controller;             // ĳ���� ��Ʈ�ѷ� ������Ʈ
    private Light[] sceneLights;                         // ���� ��� ����Ʈ�� �����ͼ� �迭�� �ִ´�.
    private int currentDirectionalLightDamage;          // ���� �𷺼ų� ����Ʈ ������
    private float lastDamageTime;                       // ���������� �������� ���� �ð�
    private float lastDirectionalLightDamageTIme;         // ���������� �𷺼ų� ����Ʈ �������� ������ �ð�
    private float cumalativeDamage;                     // ���� ������

    void Start()        // ������ �ʱ�ȭ
    {
        controller = GetComponent<CharacterController>();
        sceneLights = FindObjectsOfType<Light>();       // Scene�� ��� ����Ʈ ã��
        ResetDirectionalLightDamage();
    }

    void Update()
    {
        // �÷��̾� �̵�ó��
        Vector3 movement = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        controller.Move(movement * moveSpeed * Time.deltaTime);

        // ������ ó��
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

    void TakeDamage()               // ������ ����
    {
        int damage = CalculateDamage();
        cumalativeDamage += damage;
        lastDamageTime = Time.time;
        Debug.Log($"�÷��̾ �������� ���� : {damage} , ���� ������ : {cumalativeDamage}");
    }

    int CalculateDamage()
    {
        int damage = currentDirectionalLightDamage;

        float closestPointLightDistance = float.MaxValue;
        bool exposedToPointLight = false;

        // ���� ����� ����Ʈ ����Ʈ ã��
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

        // ����Ʈ ����Ʈ�� �Ÿ��� ���� ������ ���
        if (exposedToPointLight)
        {
            if (closestPointLightDistance <= lightDamage.nearDistance) damage += (int)lightDamage.nearDistance;
            else if (closestPointLightDistance <= lightDamage.mediumDistance) damage += (int)lightDamage.mediumDistance;
            else damage += (int)lightDamage.farDamage;
        }

        return damage;
    }

    // �÷��̾ ���� ����Ǿ��ִ��� Ȯ��
    bool IsExposedToLight()
    {
        return IsExposedToDirectionalLight() || IsExposedToAnyPointLIght();               // ����Ʈ Ÿ���� �߰��Ǹ� �߰� �ڵ� ���ش�.
    }

    // ����Ʈ ����Ʈ�� ���� �Ǿ��ִ��� Ȯ��
    bool isExposedToPointLight(Light light)
    {
        Vector3 directionalToLight = light.transform.position - transform.position;         // �޾ƿ� ����Ʈ�� ���Ⱚ�� ���Ѵ�.
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

    // �𷺼ų� ����Ʈ ������ ����
    void ResetDirectionalLightDamage()
    {
        currentDirectionalLightDamage = directionalLightDamage.baseDamage;
    }
}
