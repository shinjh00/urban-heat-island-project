using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────
// 그린 시뮬레이션의 "색칠 담당" 컴포넌트.
// 색 적용(MaterialPropertyBlock)도 이 클래스 안에서 직접 처리한다.
// → BuildingInfo 는 순수 데이터(temperature 등)만 보관하고 색칠 로직은 갖지 않음.
//
// 핵심 역할:
//   1. ApplyTemperatureColors() : 건물의 온도에 맞는 색을 입힌다. ← 내 핵심 작업
//   2. ApplyTop10Material()     : 우선녹화 top10 건물을 진한 초록으로 덮는다.
//   3. BlinkAllBuildings()      : 그리드 선택 시 건물들을 잠깐 깜빡인다.
//   4. ApplyCooledColors()      : 시뮬레이션 후 낮아진 온도로 다시 색칠한다.
//
// 온도값은 info.temperature 로 읽고/쓴다. 온도가 "어디서 오는지"(팀원 격자 매니저)는
// 이 클래스가 몰라도 된다.
// ─────────────────────────────────────────────────────────────
public class GreeneryVisualizer : MonoBehaviour
{
    [Header("색상표 (구운 TempColorTable.asset 연결)")]
    public TempColorTable colorTable;

    [Header("기준 월 (1~12, 격자 온도 데이터 시점에 맞춤)")]
    [Range(1, 12)]
    public int currentMonth = 8;

    [Header("우선녹화 top10 색 (진한 초록)")]
    public Color top10Color = new Color(0.05f, 0.35f, 0.12f);

    [Header("Blink 설정")]
    public Color blinkColor = Color.white;
    public float blinkInterval = 0.15f; // 켜짐/꺼짐 간격(초)
    public int blinkCount = 8;          // 깜빡 횟수


    // ── 색 적용 내부 헬퍼 ──
    // MaterialPropertyBlock: 머티리얼 인스턴스를 복제하지 않고 색만 바꾸는 표준 방식.
    // URP Lit 셰이더는 "_BaseColor", 빌트인은 "_Color" 사용 (없는 속성은 무시되므로 둘 다 세팅).
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private MaterialPropertyBlock _mpb;

    // 건물 하나에 색을 입힌다. (info 의 MeshRenderer 를 찾아 색만 변경)
    private void PaintBuilding(BuildingInfo info, Color color)
    {
        if (info == null) return;

        MeshRenderer mr = info.GetComponent<MeshRenderer>();
        if (mr == null) return; // 렌더러 없는 오브젝트면 무시
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        mr.GetPropertyBlock(_mpb);
        _mpb.SetColor(BaseColorID, color);
        _mpb.SetColor(ColorID, color);
        mr.SetPropertyBlock(_mpb);
    }


    // ── 1. 온도 기반 색칠 (핵심) ──
    // 건물마다 info.temperature 를 읽어 색상표에서 색을 찾아 입힌다. (온도값은 그대로)
    public void ApplyTemperatureColors(List<BuildingInfo> buildings)
    {
        if (!HasTable() || buildings == null) return;

        foreach (var info in buildings)
        {
            if (info == null) continue;
            Color color = colorTable.GetColor(currentMonth, info.temperature);
            PaintBuilding(info, color);
        }
    }


    // ── 2. top10 진한 초록 ──
    // 온도색 위에 진한 초록을 덮는다. (온도값은 건드리지 않음)
    public void ApplyTop10Material(List<BuildingInfo> top10)
    {
        if (top10 == null) return;

        foreach (var info in top10)
            PaintBuilding(info, top10Color);
    }


    // ── 3. 그리드 선택 시 깜빡임 ──
    public void BlinkAllBuildings(List<BuildingInfo> buildings)
    {
        if (buildings == null) return;
        StopAllCoroutines();
        StartCoroutine(BlinkRoutine(buildings));
    }

    private IEnumerator BlinkRoutine(List<BuildingInfo> buildings)
    {
        for (int i = 0; i < blinkCount; i++)
        {
            // 켜짐: 전부 blinkColor 로
            foreach (var info in buildings)
                PaintBuilding(info, blinkColor);
            yield return new WaitForSeconds(blinkInterval);

            // 꺼짐: 원래 온도색으로 복귀
            ApplyTemperatureColors(buildings);
            yield return new WaitForSeconds(blinkInterval);
        }
    }


    // ── 4. 시뮬레이션 후 재색칠 ──
    // 녹화로 그리드 온도가 cooledTemp 로 낮아졌을 때, 그 온도로 전체를 다시 칠한다.
    // (이때는 온도값도 cooledTemp 로 갱신된다)
    public void ApplyCooledColors(List<BuildingInfo> buildings, float cooledTemp)
    {
        if (!HasTable() || buildings == null) return;

        Color color = colorTable.GetColor(currentMonth, cooledTemp);
        foreach (var info in buildings)
        {
            if (info == null) continue;
            info.temperature = cooledTemp; // 온도값 갱신
            PaintBuilding(info, color);
        }
    }


    // ── 5. 건물 보이기 / 숨기기 ──

    // 활성화된 모든 건물을 숨긴다. (MeshRenderer 끄기. 콜라이더는 남아있어 클릭은 가능)
    public void HideAllBuildings()
    {
        if (BuildingManager.Instance == null) return;
        foreach (var kv in BuildingManager.Instance.GetActiveBuildings())
        {
            MeshRenderer mr = kv.Value != null ? kv.Value.GetComponent<MeshRenderer>() : null;
            if (mr != null) mr.enabled = false;
        }
    }

    // 주어진 건물들만 보이고 나머지는 전부 숨긴다.
    public void ShowOnly(List<BuildingInfo> buildings)
    {
        HideAllBuildings();
        if (buildings == null) return;

        foreach (var info in buildings)
        {
            if (info == null) continue;
            MeshRenderer mr = info.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = true;
        }
    }


    private bool HasTable()
    {
        if (colorTable == null)
        {
            Debug.LogWarning("[GreeneryVisualizer] colorTable 이 비어있습니다. TempColorTable.asset 을 연결하세요.");
            return false;
        }
        return true;
    }


#if UNITY_EDITOR
    // ── 테스트용 ──
    // 격자 온도 데이터가 아직 없으니, 가짜 온도를 넣어 색이 잘 입혀지는지 확인한다.
    // 플레이 모드에서 컴포넌트 우클릭 > "테스트: 더미 온도로 색칠" 실행.
    [ContextMenu("테스트: 더미 온도로 색칠 (모두 같은 온도)")]
    private void DebugApplyDummyColors()
    {
        if (!HasTable()) return;
        if (BuildingManager.Instance == null)
        {
            Debug.LogWarning("[GreeneryVisualizer] BuildingManager 가 없습니다. 플레이 모드에서 실행하세요.");
            return;
        }

        // 모든 건물에 같은 온도를 할당 (한 그리드 = 한 온도)
        float dummyTemp = 32.5f;
        var buildings = new List<BuildingInfo>();
        foreach (var kv in BuildingManager.Instance.GetActiveBuildings())
        {
            var info = kv.Value != null ? kv.Value.GetComponent<BuildingInfo>() : null;
            if (info == null) continue;

            info.temperature = dummyTemp; // 모든 건물에 32.5도
            buildings.Add(info);
        }

        ApplyTemperatureColors(buildings);
        Debug.Log($"[GreeneryVisualizer] 더미 온도 {dummyTemp}도로 {buildings.Count}개 건물 색칠 완료");
    }
#endif
}