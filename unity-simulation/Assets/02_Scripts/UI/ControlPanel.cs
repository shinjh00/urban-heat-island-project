using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControlPanel : MonoBehaviour
{
    [Header("TabGroup")]
    [SerializeField]
    private Button[] tabButtons;

    [Header("ContentGroup")]
    [SerializeField]
    private GameObject[] contentPanels;

    [Header("시각화 탭")]
    [SerializeField]
    private TMP_Dropdown dateDropdown;          // 날짜 선택 드롭다운
    [SerializeField]
    private Button visualizationResetButton;    // 시각화 데칼 초기화 버튼
    [SerializeField]
    private Button visualizationStartButton;    // 시각화 시작 버튼

    [Header("옥상 녹화 시뮬레이션 탭")]
    [SerializeField]
    private TMP_Text selectGreeneryZoneText;    // 구역 선택 안내 문구
    [SerializeField]
    private Button selectGreeneryZoneButton;    // 구역 선택 버튼
    [SerializeField]
    private TMP_Text currentRatioText;          // 슬라이더 조작 시 현재 수치 텍스트
    [SerializeField]
    private Slider selecGreeneryRatioSlider;    // 목표 녹화율 설정 슬라이더
    [SerializeField]
    private Button startGreeneryButton;         // 시뮬레이션 시작 버튼
    [SerializeField]
    private Button stopGreeneryButton;         // 시뮬레이션 시작 버튼


    // ZoneSelector로부터 전달받아 캐싱해두는 현재 선택된 zone 정보
    private ZoneData selectedZoneData;

    // 드롭다운에 들어갈 옵션 리스트
    private List<string> dateOptions = new List<string>();

    private Image checkmarkImage;

    private string selectZoneTextDefault = "시뮬레이션 할 구역을\r\n선택하세요.";

    private bool isDateSelected = false;    // 시각화 날짜 선택 여부
    private bool isVisualizing = false;     // 시각화 진행 여부
    private bool isZoneSelected = false;    // 시뮬 구역 선택 여부
    private bool isRatioSelected = false;   // 시뮬 녹화율 선택 여부
    private bool isSimulating = false;      // 시뮬 진행 여부

    void Start()
    {
        // 버튼 초기 색상 캐싱 & 버튼 리스너 등록 자동화
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i;  // SwitchTab() 실행 시 반복문때문에 항상 i=3이 되는 것을 막으려고
            tabButtons[i].onClick.AddListener(() => SwitchTab(index));
        }

        // 시작할 때 첫 번째 탭(0번)만 열기
        SwitchTab(0);

        // 드롭다운 아이템 생성 및 초기화
        InitDateDropdown();

        // 시작 시 현재 슬라이더 값 즉시 반영
        UpdatePercentValue(selecGreeneryRatioSlider.value);

        selectGreeneryZoneText.text = selectZoneTextDefault;

        // 시각화 시작 버튼 리스너 등록
        if (visualizationStartButton != null)
        {
            visualizationStartButton.onClick.AddListener(OnVisualizationStartButtonClicked);
            visualizationResetButton.gameObject.SetActive(false);
        }

        // 시각화 데칼 초기화 버튼 리스너 등록
        if (visualizationResetButton != null)
        {
            visualizationResetButton.onClick.AddListener(OnClickResetButton);
        }

        // 옥상 녹화 구역 선택 버튼 리스너 등록
        if (selectGreeneryZoneButton != null)
        {
            selectGreeneryZoneButton.onClick.AddListener(OnSelectGreeneryZoneButtonClicked);
        }

        // 옥상 녹화 시뮬레이션 시작 버튼 리스너 등록
        if (startGreeneryButton != null)
        {
            startGreeneryButton.onClick.AddListener(OnStartGreeneryButtonClicked);
            startGreeneryButton.gameObject.SetActive(true);
        }

        // 옥상 녹화 시뮬레이션 종료 버튼 리스너 등록
        if (stopGreeneryButton != null)
        {
            stopGreeneryButton.onClick.AddListener(OnStopGreeneryButtonClicked);
            stopGreeneryButton.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // 그리드(Zone) 선택 이벤트를 직접 구독하여 zoneId/temperature 캐싱
        ZoneSelector.OnZoneSelected += HandleZoneSelected;

        // 슬라이더 값이 바뀔 때 실시간으로 텍스트 업데이트하는 리스너
        if (selecGreeneryRatioSlider != null)
        {
            selecGreeneryRatioSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

 
    private void OnDisable()
    {
        ZoneSelector.OnZoneSelected -= HandleZoneSelected;

        if (selecGreeneryRatioSlider != null)
        {
            selecGreeneryRatioSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }


    #region `` 시각화 ``
    private void SwitchTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= contentPanels.Length)
            return;

        for (int i = 0; i < contentPanels.Length; i++)
        {
            // 선택한 인덱스의 패널만 true, 나머지는 false
            contentPanels[i].SetActive(i == tabIndex);

            // 선택한 탭 버튼의 색상 변경
            if (tabButtons[i].TryGetComponent<Image>(out Image btnImage))
            {
                if (i == tabIndex)
                    btnImage.color = new Color(1.0f, 1.0f, 1.0f, 0.98f);
                else
                    btnImage.color = new Color(1.0f, 1.0f, 1.0f, 0.80f);
            }
        }
    }

    // 실행 시 시각화 드롭다운 초기화
    private void InitDateDropdown()
    {
        if (dateDropdown == null)
            return;

        dateOptions.Clear();
        dateDropdown.ClearOptions();

        // 추가할 글자들을 담을 리스트
        DateTime currentTime = new DateTime(2026, 6, 1);
        DateTime endTime = new DateTime(2025, 1, 1);
        while (currentTime >= endTime)
        {
            // "2026년 06월" 형태로 문자열 만들기
            dateOptions.Add(currentTime.ToString("yyyy년 MM월"));
            // 한 달 빼기
            currentTime = currentTime.AddMonths(-1);
        }

        // 생성한 리스트를 드롭다운에 넣기
        dateDropdown.AddOptions(dateOptions);
        dateDropdown.RefreshShownValue();  // 화면 갱신
        isDateSelected = false;

        dateDropdown.captionText.text = "년/월 선택";
        Transform template = dateDropdown.transform.Find("Template");
        checkmarkImage = template.GetComponentInChildren<Toggle>(true)?.graphic as Image;
        checkmarkImage.enabled = false;

        // 드롭다운 리스너 연결
        dateDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }


    // 드롭다운 아이템을 선택했을 때 실행될 기능
    private void OnDropdownValueChanged(int index)  // isDateSelected
    {
        isDateSelected = true;
        checkmarkImage.enabled = true;

        if (isVisualizing)
            visualizationStartButton.interactable = true;
        Debug.Log($"================== {index} =================");

        // dateDropdown.options[index].text로 선택한 "2026년 06월" 글자를 가져옴
        string originalText = dateDropdown.options[index].text;
        string yearText = originalText.Substring(0, 4);     // 0번째부터 4개
        string monthText = originalText.Substring(6, 2);    // 6번째부터 2개

        string selectedDateText = yearText + monthText + 151400;
        Debug.Log($"[ControlPanel] 선택된 날짜: {selectedDateText}");

        UIManager.Instance.SetSelectedDate(selectedDateText);
    }
    #endregion


    #region `` 시뮬레이션 ``
    // 그리드가 선택될 때마다 호출되어 zone 정보를 캐싱
    private void HandleZoneSelected(ZoneItem selectedItem)
    {
        if (selectedItem == null || selectedItem.data == null)
        {
            isZoneSelected = false;
            return;
        }

        isZoneSelected = true;
        selectedZoneData = selectedItem.data;
        selectGreeneryZoneText.text = selectedZoneData.zoneId;
        Debug.Log($"[ControlPanel] 선택된 Zone 캐싱 완료 — ID: {selectedZoneData.zoneId}, 온도: {selectedZoneData.temperature}");
    }

    private void OnSliderValueChanged(float value)
    {
        isRatioSelected = (value == 0) ? false : true;
        UpdatePercentValue(value);
        startGreeneryButton.gameObject.SetActive(true);
        stopGreeneryButton.gameObject.SetActive(false);
    }

    // 목표 녹화율 슬라이더 움직일 시 숫자 텍스트 출력
    private void UpdatePercentValue(float value)
    {
        value = value * 100;
        int intValue = Mathf.RoundToInt(value);
        currentRatioText.text = $"{intValue}%";
    }
    #endregion


    #region `` 버튼 클릭 리스너 ``

    // 시각화 시작 버튼을 눌렀을 때 실행될 함수
    private void OnVisualizationStartButtonClicked()
    {
        // 날짜를 아직 한 번도 선택 안 했다면 서버 요청 블로킹 + 경고메세지
        if (!isDateSelected)
        {
            UIManager.Instance.ShowWarningMessage("날짜를 선택해 주세요.");
            return;
        }

        UIManager.Instance.screenBlockingPanel.SetActive(true);

        // NetworkManager에게 현재 선택된 날짜로 새로고침하도록 요청
        NetworkManager.Instance.RefreshDecalData();
        isVisualizing = true;
        visualizationStartButton.interactable = false;
        visualizationResetButton.gameObject.SetActive(true);
    }

    // 시각화 데칼 초기화 버튼을 눌렀을 때 실행될 함수
    public void OnClickResetButton()
    {
        NetworkManager.Instance.ResetDecalData();
        visualizationResetButton.gameObject.SetActive(false);
        visualizationStartButton.interactable = true;
        InitDateDropdown();
        //isVisualizing = false;
    }

    // 옥상 녹화 구역 선택 버튼을 눌렀을 때 실행될 함수
    private void OnSelectGreeneryZoneButtonClicked()
    {
        ZoneManager.Instance.EnableZoneSelection();
        startGreeneryButton.gameObject.SetActive(true);
        stopGreeneryButton.gameObject.SetActive(false);
    }

    // 옥상 녹화 시뮬레이션 시작 버튼을 눌렀을 때 실행될 함수
    private void OnStartGreeneryButtonClicked()
    {
        // 구역이 선택되지 않았다면 진행 불가 + 경고메세지
        if (!isZoneSelected)
        {
            UIManager.Instance.ShowWarningMessage("구역을 선택해 주세요.");
            return;
        }

        // 목표 녹화율이 선택되지 않았다면 진행 불가 + 경고메세지
        if (!isRatioSelected)
        {
            UIManager.Instance.ShowWarningMessage("목표 녹화율을 설정해 주세요.");
            return;
        }

        if (isZoneSelected && isRatioSelected)
        {
            // 슬라이더에서 목표 녹화율 값 읽기
            float greeneryRate = selecGreeneryRatioSlider != null ? selecGreeneryRatioSlider.value : 0f;

            Debug.Log($"[ControlPanel] 옥상 녹화 시뮬레이션 시작 — Zone: {selectedZoneData.zoneId}, " +
                $"온도: {selectedZoneData.temperature}, 목표 녹화율: {greeneryRate}%");

            // 캐싱해둔 두 정보(zone + 녹화율)를 한 번에 SimulationController로 전달
            SimulationController.Instance.StartGreenerySimulationForZone(
                selectedZoneData.zoneId,
                selectedZoneData.temperature,
                greeneryRate
            );

            isSimulating = true;
            startGreeneryButton.gameObject.SetActive(false);
            stopGreeneryButton.gameObject.SetActive(true);
        }
    }

    // 옥상 녹화 시뮬레이션 종료 버튼을 눌렀을 때 실행될 함수
    private void OnStopGreeneryButtonClicked()
    {
        BuildingManager.Instance.ResetAllBuildings();
        UIManager.Instance.HideResultInfo();
        isZoneSelected = false;
        isRatioSelected = false;
        selectedZoneData = null;
        selectGreeneryZoneText.text = selectZoneTextDefault;
        selecGreeneryRatioSlider.value = 0f;
        startGreeneryButton.gameObject.SetActive(true);
        stopGreeneryButton.gameObject.SetActive(false);
        isSimulating = false;
    }
    #endregion
}
