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
    private TMP_Dropdown dateDropdown;
    [SerializeField]
    private Button visualizationStartButton;

    [Header("옥상 녹화 시뮬레이션 탭")]
    [SerializeField]
    private Button selectGreeneryZoneButton; //구역 선택가능 버튼
    [SerializeField]
    private Slider selecGreeneryRatioSlider; //목표 녹화율 설정 슬라이더
    [SerializeField]
    private Button startGreeneryButton; // 시뮬레이션 시작 버튼

    [Header("그늘막 설치 시뮬레이션 탭")]
    [SerializeField]
    private Button installShadeButton;

    // ZoneSelector로부터 전달받아 캐싱해두는 현재 선택된 zone 정보
    private ZoneData selectedZoneData;

    // 드롭다운에 들어갈 옵션 리스트
    private List<string> dateOptions = new List<string>();

    private bool isDateSelected;


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

        // 시각화 시작 버튼 리스너 등록
        if (visualizationStartButton != null)
        {
            visualizationStartButton.onClick.AddListener(OnVisualizationStartButtonClicked);
        }

        // 옥상 녹화 시뮬레이션 시작 버튼 리스너 등록
        if (selectGreeneryZoneButton != null)
        {
            selectGreeneryZoneButton.onClick.AddListener(OnSelectGreeneryZoneButtonClicked);
        }

        // 옥상 녹화 시뮬레이션 시작 버튼 리스너 등록
        if (startGreeneryButton != null)
        {
            startGreeneryButton.onClick.AddListener(OnStartGreeneryButtonClicked);
        }
    }

    private void OnEnable()
    {
        // 그리드(Zone) 선택 이벤트를 직접 구독하여 zoneId/temperature 캐싱
        ZoneSelector.OnZoneSelected += HandleZoneSelected;
    }

    private void OnDisable()
    {
        ZoneSelector.OnZoneSelected -= HandleZoneSelected;
    }

    // 그리드가 선택될 때마다 호출되어 zone 정보를 캐싱
    private void HandleZoneSelected(ZoneItem selectedItem)
    {
        if (selectedItem == null || selectedItem.data == null) return;

        selectedZoneData = selectedItem.data;
        Debug.Log($"[ControlPanel] 선택된 Zone 캐싱 완료 — ID: {selectedZoneData.zoneId}, 온도: {selectedZoneData.temperature}");
    }


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
                    btnImage.color = new Color(1.0f, 1.0f, 1.0f, 0.90f);
            }
        }
    }

    // 실행 시 시각화 드롭다운 초기화
    private void InitDateDropdown()
    {
        if (dateDropdown == null)
            return;

        dateDropdown.ClearOptions();
        dateOptions.Clear();

        isDateSelected = false;
        dateOptions.Add("년/월 선택");

        // 추가할 글자들을 담을 리스트
        DateTime currentTime = new DateTime(2026, 6, 1);
        DateTime endTime = new DateTime(2025, 1, 1);
        while (currentTime >= endTime)
        {
            // "2026년 06월" 형태로 문자열 만들기
            string dateText = currentTime.ToString("yyyy년 MM월");
            dateOptions.Add(dateText);
            // 한 달 빼기
            currentTime = currentTime.AddMonths(-1);
        }

        // 생성한 리스트를 드롭다운에 넣기
        dateDropdown.AddOptions(dateOptions);
        //dateDropdown.value = 0;  // 0번 인덱스를 default로
        dateDropdown.RefreshShownValue();  // 화면 갱신

        // 드롭다운 리스너 연결
        dateDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    // 드롭다운 아이템을 선택했을 때 실행될 기능
    private void OnDropdownValueChanged(int index)  // isDateSelected
    {
        // 🌟 [최초 선택] 아직 날짜를 선택하지 않은 상태일 때
        if (!isDateSelected)
        {
            // 드롭다운을 열었다가 아무것도 안 고르고 닫았을 때 (index가 여전히 0일 때)는 무시합니다.
            if (index == 0) return;

            // 유저가 진짜 날짜를 골랐다면, 선택한 글자를 기억합니다.
            string selectedTextTemp = dateDropdown.options[index].text;

            // 🌟 유저가 고른 시점에 0번째인 '년/월 선택'을 리스트에서 영구 제거합니다.
            dateDropdown.options.RemoveAt(0);

            // 플래그를 true로 바꾸어 다음부턴 이 조건문에 들어오지 못하게 만듭니다.
            isDateSelected = true;

            // 0번 방이 사라졌으니 새로 정렬된 리스트에서 유저가 골랐던 텍스트의 방 번호를 다시 찾습니다.
            int newIndex = dateDropdown.options.FindIndex(option => option.text == selectedTextTemp);

            // value 변경 시 발생하는 중복 호출(무한루프) 방지용 안전장치
            dateDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            dateDropdown.value = newIndex;
            dateDropdown.RefreshShownValue();
            dateDropdown.onValueChanged.AddListener(OnDropdownValueChanged);

            // 인덱스를 새로 찾은 번호로 보정해 줍니다.
            index = newIndex;
        }

        // dateDropdown.options[index].text로 선택한 "2026년 06월" 글자를 가져옴
        string originalText = dateDropdown.options[index].text;

        string yearText = originalText.Substring(0, 4);     // 0번째부터 4개
        string monthText = originalText.Substring(6, 2);    // 6번째부터 2개

        string selectedDateText = yearText + monthText + 151400;
        Debug.Log($"[ControlPanel] 선택된 날짜: {selectedDateText}");

        UIManager.Instance.SetSelectedDate(selectedDateText);
    }

    // 시각화 시작 버튼을 눌렀을 때 실행될 함수
    private void OnVisualizationStartButtonClicked()
    {
        Debug.Log("[NetworkManager] 시각화 시작. 서버에 데이터 요청을 보냅니다.");
        // NetworkManager에게 현재 선택된 날짜로 새로고침하도록 요청
        NetworkManager.Instance.RefreshDecalData();
    }

    // 옥상 녹화 구역 선택 버튼을 눌렀을 때 실행될 함수
    private void OnSelectGreeneryZoneButtonClicked()
    {
        ZoneManager.Instance.EnableZoneSelection();
    }

    // 옥상 녹화 시뮬레이션 시작 버튼을 눌렀을 때 실행될 함수
    private void OnStartGreeneryButtonClicked()
    {
        // 1. 구역이 아직 선택되지 않았다면 진행 불가
        if (selectedZoneData == null)
        {
            Debug.LogWarning("[ControlPanel] 먼저 구역(그리드)을 선택해주세요.");
            return;
        }

        // 2. 슬라이더에서 목표 녹화율 값 읽기
        float greeneryRate = selecGreeneryRatioSlider != null ? selecGreeneryRatioSlider.value : 0f;

        Debug.Log($"[ControlPanel] 옥상 녹화 시뮬레이션 시작 — Zone: {selectedZoneData.zoneId}, " +
            $"온도: {selectedZoneData.temperature}, 목표 녹화율: {greeneryRate}%");

        // 3. 캐싱해둔 두 정보(zone + 녹화율)를 한 번에 SimulationController로 전달
        SimulationController.Instance.StartGreenerySimulationForZone(
            selectedZoneData.zoneId,
            selectedZoneData.temperature,
            greeneryRate
        );
    }

}
