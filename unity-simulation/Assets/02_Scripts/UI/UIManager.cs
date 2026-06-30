using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Building Info Panel")]
    public GameObject infoPanel;
    public TMP_Text textArea1;
    public TMP_Text textArea2;
    public TMP_Text textArea3;
    public TMP_Text textArea4;
    public TMP_Text textArea5;


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

        // 실행 시 EventSystem 찾기 (없으면 할당)
    }

    #region ``Building Info Panel 제어``
    // BuildingSelector에서 HandleBuildingSelected() 실행 시 호출
    public void ShowBuildingInfo(BuildingInfo info)
    {
        currentBuildingInfo = info;

        if (infoPanel != null)
            ShowPanel(infoPanel);

        if (textArea1 != null)
            textArea1.text = "건물ID : " + info.data.id;
        if (textArea2 != null)
            textArea2.text = "면적 : " + info.data.areaSize;
        if (textArea3 != null)
            textArea3.text = "높이 : " + info.data.height;
        if (textArea4 != null)
            textArea4.text = "ZoneID : " + info.zoneId;
        if (textArea5 != null)
            textArea5.text = "온도 : " + info.temperature;

        Debug.Log($"[UIManager] BuildingInfo 출력 {info.data.id} {info.zoneId}");
    }

    // BuildingSelector에서 HandleBuildingDeselected() 실행 시 호출
    public void HideBuildingInfo()
    {
        if (currentBuildingInfo != null)
            currentBuildingInfo = null;

        if (textArea1 != null) textArea1.text = string.Empty;
        if (textArea2 != null) textArea2.text = string.Empty;
        if (textArea3 != null) textArea3.text = string.Empty;
        if (textArea4 != null) textArea4.text = string.Empty;
        if (textArea5 != null) textArea5.text = string.Empty;

        HidePanel(infoPanel);
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
