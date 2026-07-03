using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using static UnityEditor.Progress;
#endif

// 그리드 색상 변경 역할
public class ZoneRenderer : MonoBehaviour
{
    // 생성된 개별 머티리얼의 목록 (추후 메모리 해제를 위해 관리)
    public List<Material> generatedMaterials = new List<Material>();

    // 생성된 원본 머티리얼을 저장하여 인스턴스화할 때 참조
    private Material generatedBaseMaterial;

    // Blink 전체 시간
    [SerializeField]
    private float duration;
    // Blink 깜빡이는 간격
    [SerializeField]
    private float interval;

    // URP 또는 Standard 셰이더를 찾아 베이스 머티리얼을 생성
    public void InitRuntimeMaterial()
    {
        Shader zoneShader = Shader.Find("Universal Render Pipeline/Lit");
        bool isURP = true;

        if (zoneShader == null)
        {
            zoneShader = Shader.Find("Standard");
            isURP = false;
        }

        generatedBaseMaterial = new Material(zoneShader);
        generatedBaseMaterial.name = "Runtime_WeatherGrid_BaseMat";

        // URP/Standard에 맞는 투명도(Transparent) 렌더링 설정
        if (isURP)
        {
            generatedBaseMaterial.SetFloat("_Surface", 1);
            generatedBaseMaterial.SetFloat("_Blend", 0);
            generatedBaseMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            generatedBaseMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            generatedBaseMaterial.SetInt("_ZWrite", 0);
            generatedBaseMaterial.DisableKeyword("_ALPHATEST_ON");
            generatedBaseMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            generatedBaseMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            generatedBaseMaterial.SetFloat("_Mode", 3);
            generatedBaseMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            generatedBaseMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            generatedBaseMaterial.SetInt("_ZWrite", 0);
            generatedBaseMaterial.DisableKeyword("_ALPHATEST_ON");
            generatedBaseMaterial.EnableKeyword("_ALPHABLEND_ON");
            generatedBaseMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        Debug.Log("[ZoneRenderer] InitRuntimeMaterial 실행. 그리드 베이스 머티리얼을 생성");
    }


    // 그리드 머티리얼을 생성하고 렌더러에 적용
    public void ApplyTemperatureStyle(MeshRenderer mr, float temp)
    {
        // 베이스 머티리얼을 복제하여 고유 머티리얼 생성
        Material uniqueMat = Instantiate(generatedBaseMaterial);

        // 색상 지정 (온도를 기반으로 하거나, 원하는 고정 색상을 설정)
        Color targetColor = Color.blue;
        targetColor.a = 0.45f;  // 투명도 설정 (InitRuntimeMaterial에서 설정한 투명도와 함께 작용)

        // 셰이더 프로퍼티 이름 확인 후 적용
        if (uniqueMat.HasProperty("_BaseColor"))
            uniqueMat.SetColor("_BaseColor", targetColor); // URP용
        else
            uniqueMat.SetColor("_Color", targetColor);     // Standard용

        // 생성된 머티리얼을 렌더러에 할당하고 리스트에 보관
        mr.material = uniqueMat;
        generatedMaterials.Add(uniqueMat);
    }


    // Zone 선택 시 깜빡이는 코루틴 (duration : 깜빡임 지속 시간)
    public IEnumerator BlinkEffect(MeshRenderer mr)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 0.2초 간격으로 렌더러 활성/비활성 토글
            mr.enabled = !mr.enabled;
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        // 깜빡임 종료 후 렌더러를 켜진 상태로 복원
        mr.enabled = true;
    }


    // 스크립트 삭제 시 생성했던 모든 머티리얼 자원 정리 (메모리 누수 방지)
    private void OnDestroy()
    {
        // 개별 생성된 머티리얼 해제
        foreach (Material mat in generatedMaterials) if (mat != null) Destroy(mat);
        // 베이스 머티리얼 해제
        if (generatedBaseMaterial != null) Destroy(generatedBaseMaterial);
    }

}
