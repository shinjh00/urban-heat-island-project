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

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        // UI 영역 클릭 시 건물 Raycast 실행하지 않도록
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // 카메라에서 마우스 방향으로 Ray 쏘기
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            BuildingInfo info = hit.collider.GetComponent<BuildingInfo>();

            if (info != null)
            {
                // 건물에 Ray가 닿으면 BuildingManager에 선택 전달하고 끝내기
                BuildingManager.Instance.SelectBuilding(info);
                return;
            }
        }

        // 건물이 아닌 곳을 클릭한 경우 선택을 해제
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
}