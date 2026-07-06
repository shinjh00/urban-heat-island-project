using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Camera Toggle Panel")]
    public GameObject cameraTogglePanel;        // 카메라 이동 안내 패널
    public TMP_Text cameraToggleText;           // 카메라 이동 ON/OFF 메세지

    [Header("Warning Message Panel")]
    public GameObject warningMessagePanel;      // 경고 메세지 출력 패널
    public TMP_Text warningMessageText;         // 경고 메세지

    [Header("Building Info Panel")]
    public BuildingInfoPanel buildingInfoPanel; // 건물 정보 출력 패널

    [Header("Building Info Panel")]
    public ResultPanel simulationResultPanel;   // 결과 보고서 출력 패널

    [Header("Screen Blocking Panel")]
    public GameObject screenBlockingPanel;      // 로딩 중 상호작용 안 되게 화면 전체를 덮는 패널

    // 시각화 드롭다운에서 선택된 날짜를 전역적으로 기억할 변수
    public string CurrentSelectedDate { get; private set; }

    // 현재 선택된 건물 데이터
    private BuildingInfo currentBuildingInfo;

    // 경고 메세지 Show/Hide 코루틴을 기억할 변수
    private Coroutine warningHideCoroutine;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 실행 시 EventSystem 찾고 없으면 할당
        if (EventSystem.current != null)
        {
            return;
        }
        EventSystem eventSystem = Resources.Load<EventSystem>("EventSystem");
        Instantiate(eventSystem);
    }

    private void Start()
    {
        warningMessagePanel.gameObject.SetActive(false);
        buildingInfoPanel.gameObject.SetActive(false);
        simulationResultPanel.gameObject.SetActive(false);
        screenBlockingPanel.SetActive(false);
    }


    // 카메라 움직임 ON/OFF 여부 메세지 출력
    public void CameraToggleText(string status)
    {
        cameraToggleText.text = status;
    }

    // 화면 왼쪽 상단에 경고 메세지 출력
    public void ShowWarningMessage(string msg)
    {
        if (warningMessagePanel == null || warningMessageText == null)
            return;

        warningMessageText.text = msg;
        warningMessagePanel.gameObject.SetActive(true);

        if (warningHideCoroutine != null)
        {
            StopCoroutine(warningHideCoroutine);
        }
        warningHideCoroutine = StartCoroutine(HideWarningPanelAfterDelay());
    }

    // 일정 시간 이후 경고 메세지를 자동으로 끄는 코루틴
    private IEnumerator HideWarningPanelAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        warningMessagePanel.gameObject.SetActive(false);
        warningMessageText.text = string.Empty;
        warningHideCoroutine = null;
    }


    // 시각화 드롭다운에서 날짜 선택 시 호출
    public void SetSelectedDate(string dateText)
    {
        CurrentSelectedDate = dateText;
        Debug.Log($"[UIManager] 시각화 - 선택된 날짜 {CurrentSelectedDate}");
    }


    #region `` Show/Hide Panel ``

    // BuildingSelector에서 HandleBuildingSelected() 실행 시 호출
    public void ShowBuildingInfoPanel(BuildingInfo info)
    {
        currentBuildingInfo = info;

        if (buildingInfoPanel != null)
            buildingInfoPanel.gameObject.SetActive(true);
        else
            Debug.LogWarning("[UIManager] buildingInfoPanel이 할당되지 않았습니다.");
    }

    // BuildingSelector에서 HandleBuildingDeselected() 실행 시 호출
    public void HideBuildingInfoPanel()
    {
        currentBuildingInfo = null;

        if (buildingInfoPanel != null)
            buildingInfoPanel.gameObject.SetActive(false);
        else
            Debug.LogWarning("[UIManager] buildingInfoPanel이 할당되지 않았습니다.");
    }

    // SimulationController에서 HandleBuildingSelected() 실행 시 호출
    public void ShowResultPanel()
    {
        if (simulationResultPanel != null)
            simulationResultPanel.gameObject.SetActive(true);
        else
            Debug.LogWarning("[UIManager] simulationResultPanel이 할당되지 않았습니다.");
    }

    // BuildingSelector에서 HandleBuildingDeselected() 실행 시 호출
    public void HideResultPanel()
    {
        if (simulationResultPanel != null)
            simulationResultPanel.gameObject.SetActive(false);
        else
            Debug.LogWarning("[UIManager] simulationResultPanel이 할당되지 않았습니다.");
    }
    #endregion


    


}
