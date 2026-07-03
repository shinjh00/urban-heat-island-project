using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Building Info Panel")]
    public BuildingInfoPanel buildingInfoPanel;

    // 시각화 드롭다운에서 선택된 날짜를 전역적으로 기억할 변수
    public string CurrentSelectedDate { get; private set; }


    // 현재 선택된 건물 데이터
    private BuildingInfo currentBuildingInfo;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 실행 시 EventSystem 찾고 없으면 할당

        // 시각화 날짜 초기화 방어 코드
        CurrentSelectedDate = "2026년 06월";
    }

    #region ``시각화 기능``
    public void SetSelectedDate(string dateText)
    {
        CurrentSelectedDate = dateText;
        Debug.Log($"[UIManager] 시각화 - 선택된 날짜 {CurrentSelectedDate}");
    }

    #endregion

    #region ``Building Info Panel 제어``
    // BuildingSelector에서 HandleBuildingSelected() 실행 시 호출
    public void ShowBuildingInfo(BuildingInfo info)
    {
        currentBuildingInfo = info;
        // 분리된 패널 스크립트에게 UI 갱신 및 오픈 처리를 위임합니다.
        if (buildingInfoPanel != null)
        {
            buildingInfoPanel.Show(info);
        }
        else
        {
            Debug.LogWarning("[UIManager] buildingInfoPanel이 할당되지 않았습니다.");
        }
    }

    // BuildingSelector에서 HandleBuildingDeselected() 실행 시 호출
    public void HideBuildingInfo()
    {
        currentBuildingInfo = null;

        // 패널 닫기 및 초기화 위임
        if (buildingInfoPanel != null)
        {
            buildingInfoPanel.Hide();
        }
    }
    #endregion


    #region `` Result Panel 제어``
    public void ShowResult()
    {
        //ShowPanel(resultPanel);
    }

    public void HideResult()
    {
        //HidePanel(resultPanel);
    }
    #endregion


    private void ShowPanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(true);
    }

    private void HidePanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(false);
    }

}
