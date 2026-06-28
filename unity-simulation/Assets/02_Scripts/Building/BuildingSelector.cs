using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 마우스 클릭으로 건물을 선택하는 클래스

public class BuildingSelector : MonoBehaviour
{
    // 선택 시 하이라이트 색상
    public Color highlightColor = new Color(0.8f, 0.8f, 0.8f);

    // 선택된 건물의 MeshRenderer와 Material
    private MeshRenderer selectedRenderer;
    //private Material selectedMaterial;

    // default 색상 기억용 변수
    private Color originalColor;

    [Header("구역 선택 (시뮬레이션용)")]
    public GreeneryVisualizer greeneryVisualizer;

    // '구역 선택' 버튼을 누르면 true. 테스트 땐 직접 체크해도 됨
    public bool zoneSelectionMode = false;

    // 선택된 구역의 건물들 (SimulationController 가 가져감)
    [HideInInspector]
    public List<BuildingInfo> selectedZoneBuildings = new List<BuildingInfo>();

    private const double CELL_SIZE = 500.0;    // 500m 그리드
    private const double LAT_SCALE = 111320.0; // 위도 1도 ≈ 111320m

    private void Awake()
    {
        // BuildingManager가 발행하는 선택/해제 이벤트 구독
        BuildingManager.OnBuildingSelected += HandleBuildingSelected;
        BuildingManager.OnBuildingDeselected += HandleBuildingDeselected;
    }

    private void OnDisable()
    {
        // BuildingManager가 발행하는 선택/해제 이벤트 구독 해제
        BuildingManager.OnBuildingSelected -= HandleBuildingSelected;
        BuildingManager.OnBuildingDeselected -= HandleBuildingDeselected;
    }


    // 구역선택 모드 추가(Udate수정함)
    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            BuildingInfo info = hit.collider.GetComponent<BuildingInfo>();

            if (info != null)
            {
                // 🔹 구역 선택 모드면: 단일 선택 대신 그리드 전체 선택
                if (zoneSelectionMode)
                {
                    SelectZone(info);
                    return;
                }

                // 일반 모드: 기존 단일 건물 선택 (그대로)
                BuildingManager.Instance.SelectBuilding(info);
                return;
            }
        }

        // 구역 선택 모드 중에는 빈 곳 클릭으로 해제하지 않음
        if (!zoneSelectionMode)
            BuildingManager.Instance.DeselectBuilding();
    }



    private void HandleBuildingSelected(BuildingInfo info)
    {
        // 기존 선택 건물이 있다면 복구
        RemoveHighlight();

        GameObject obj = BuildingManager.Instance.GetBuilding(info.data.id);
        if (obj == null)
            return;

        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        if (mr == null)
            return;

        selectedRenderer = obj.GetComponent<MeshRenderer>();
        if (selectedRenderer != null)
        {
            // 3. 원래 색상 저장 후 변경
            originalColor = selectedRenderer.material.color;
            selectedRenderer.material.color = highlightColor;
        }

         UIManager.Instance.ShowBuildingInfo(info);
    }

    private void HandleBuildingDeselected()
    {
        RemoveHighlight();
        UIManager.Instance.HideInfoPanel();
    }

    private void RemoveHighlight()
    {
        if (selectedRenderer != null)
        {
            // 4. 저장해둔 원래 색상으로 복구
            selectedRenderer.material.color = originalColor;
            selectedRenderer = null;
        }
    }

    // 매서드 추가부분
    // '구역 선택' 버튼이 호출 (UI 버튼 OnClick 에 연결)
    public void EnableZoneSelection()
    {
        zoneSelectionMode = true;
        Debug.Log("[BuildingSelector] 구역 선택 모드 ON — 건물을 클릭하세요");
    }

    // 클릭한 건물이 속한 그리드 전체를 선택: 그 구역만 보이고 깜빡
    private void SelectZone(BuildingInfo clicked)
    {
        if (clicked.data == null) return;

        selectedZoneBuildings = GetSameZoneBuildings(clicked);

        if (greeneryVisualizer != null)
        {
            greeneryVisualizer.ShowOnly(selectedZoneBuildings);      // 그 구역만 보이기
            greeneryVisualizer.BlinkAllBuildings(selectedZoneBuildings); // 잠깐 깜빡
        }

        Debug.Log($"[BuildingSelector] 구역 선택 완료 — {selectedZoneBuildings.Count}개 건물");
        zoneSelectionMode = false; // 한 구역 선택했으면 모드 끄기
    }

    // 클릭한 건물과 같은 500m 그리드 셀의 건물들 반환
    // [임시] zoneID 나오면 아래 셀 계산 대신 → if (info.zoneID == clicked.zoneID) 로 교체
    private List<BuildingInfo> GetSameZoneBuildings(BuildingInfo clicked)
    {
        var result = new List<BuildingInfo>();
        if (BuildingManager.Instance == null) return result;

        double cosLat = Math.Cos(clicked.data.lat * Math.PI / 180.0);
        (long, long) targetCell = GetGridCell(clicked.data.lon, clicked.data.lat, cosLat);

        foreach (var kv in BuildingManager.Instance.GetActiveBuildings())
        {
            BuildingInfo info = kv.Value != null ? kv.Value.GetComponent<BuildingInfo>() : null;
            if (info == null || info.data == null) continue;

            if (GetGridCell(info.data.lon, info.data.lat, cosLat) == targetCell)
                result.Add(info);
        }
        return result;
    }

    // 위경도 → 500m 그리드 셀 인덱스 (같은 cosLat 기준으로 경계 일관성 유지)
    private (long, long) GetGridCell(double lon, double lat, double cosLat)
    {
        double xMeter = lon * LAT_SCALE * cosLat;
        double zMeter = lat * LAT_SCALE;
        return ((long)Math.Floor(xMeter / CELL_SIZE), (long)Math.Floor(zMeter / CELL_SIZE));
    }

}