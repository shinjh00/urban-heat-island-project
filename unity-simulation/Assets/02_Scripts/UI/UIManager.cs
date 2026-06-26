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


    private BuildingInfo currentBuildingInfo;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // UIManager.Instance.ShowBuildingInfo(data);
    // BuildingSelector에 HandleBuildingSelected
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
            textArea4.text = "ZoneID : " + info.zoneID;
        if (textArea5 != null)
            textArea5.text = "온도 : " + info.temperature;

        Debug.Log($"[UIManager] BuildingInfo 출력 {info.data.id} {info.zoneID}");
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(true);
    }

    public void HideInfoPanel()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    // HidePanel() : 패널 닫기



}
