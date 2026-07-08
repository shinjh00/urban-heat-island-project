using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 마우스 클릭으로 건물을 선택하는 클래스

public class BuildingSelector : MonoBehaviour
{
    // 선택 시 하이라이트 색상
    public Color highlightColor = new Color(30f / 255f, 144f / 255f, 255f / 255f);

    // 선택된 건물의 MeshRenderer와 Material
    private MeshRenderer selectedRenderer;
    //private Material selectedMaterial;

    [Header("구역 선택 (시뮬레이션용)")]
    public GreeneryVisualizer greeneryVisualizer;


    // 머티리얼 복제 없이 색상만 덮어쓰는 프로퍼티 블록 (재사용)
    private MaterialPropertyBlock propBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP용
    private static readonly int ColorId = Shader.PropertyToID("_Color");         // Standard용

    // '구역 선택' 버튼을 누르면 true. 테스트 땐 직접 체크해도 됨
    public bool zoneSelectionMode = false;

    // 선택된 구역의 건물들 (SimulationController 가 가져감)
    [HideInInspector]
    public List<BuildingInfo> selectedZoneBuildings = new List<BuildingInfo>();

    private const double CELL_SIZE = 500.0;    // 500m 그리드
    private const double LAT_SCALE = 111320.0; // 위도 1도 ≈ 111320m

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();
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
        // 기존 선택 건물이 있다면 색상 복구
        RemoveHighlight();

        GameObject obj = BuildingManager.Instance.GetBuilding(info.data.id);
        if (obj == null)
            return;

        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        if (mr == null)
            return;

        selectedRenderer = mr;

        int colorId = mr.sharedMaterial.HasProperty(BaseColorId) ? BaseColorId : ColorId;
        propBlock.Clear();
        propBlock.SetColor(colorId, highlightColor);
        mr.SetPropertyBlock(propBlock);
        
        UIManager.Instance.ShowBuildingInfoPanel(info);
    }

    private void HandleBuildingDeselected()
    {
        RemoveHighlight();
        UIManager.Instance.HideBuildingInfoPanel();
    }

    private void RemoveHighlight()
    {
        if (selectedRenderer != null)
        {
            // 블록을 비워서 다시 적용 → 머티리얼 본래 색이 그대로 드러남
            propBlock.Clear();
            selectedRenderer.SetPropertyBlock(propBlock);
            selectedRenderer = null;
        }
    }

}