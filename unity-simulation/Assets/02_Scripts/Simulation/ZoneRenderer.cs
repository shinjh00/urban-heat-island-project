using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 그리드 색상 변경 역할
public class ZoneRenderer : MonoBehaviour
{
    // 생성된 원본 머티리얼을 저장하여 인스턴스화할 때 참조
    private Material generatedBaseMaterial;

    // 생성된 개별 머티리얼의 목록 (추후 메모리 해제를 위해 관리)
    public List<Material> generatedMaterials = new List<Material>();



    // URP 또는 Standard 셰이더를 찾아 베이스 머티리얼을 생성
    public void InitRuntimeMaterial()
    {
        Shader weatherShader = Shader.Find("Universal Render Pipeline/Lit");
        bool isURP = true;

        if (weatherShader == null)
        {
            weatherShader = Shader.Find("Standard");
            isURP = false;
        }

        generatedBaseMaterial = new Material(weatherShader);
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
    }

    
    /// <summary>
    /// Zone 선택 시 깜빡이는 코루틴
    /// </summary>
    /// <param name="mr">깜빡일 오브젝트의 MeshRenderer</param>
    /// <param name="duration">깜빡임 지속 시간</param>
    public IEnumerator BlinkEffect(MeshRenderer mr, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 0.2초 간격으로 렌더러 활성/비활성 토글
            mr.enabled = !mr.enabled;
            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }
        // 깜빡임 종료 후 렌더러를 켜진 상태로 복원
        mr.enabled = true;
    }


    // 그리드 머티리얼을 생성하고 렌더러에 적용
    public void ApplyTemperatureStyle(MeshRenderer mr, float temp)
    {
        // 베이스 머티리얼을 복제하여 고유 머티리얼 생성
        Material uniqueMat = Instantiate(generatedBaseMaterial);

        // 생성된 머티리얼을 렌더러에 할당하고 리스트에 보관
        mr.material = uniqueMat;
        generatedMaterials.Add(uniqueMat);
    }


    // 스크립트 삭제 시 생성했던 모든 머티리얼 자원 정리 (메모리 누수 방지)
    private void OnDestroy()
    {
        // 개별 생성된 머티리얼 해제
        foreach (var mat in generatedMaterials) if (mat != null) Destroy(mat);
        // 베이스 머티리얼 해제
        if (generatedBaseMaterial != null) Destroy(generatedBaseMaterial);
    }

}
