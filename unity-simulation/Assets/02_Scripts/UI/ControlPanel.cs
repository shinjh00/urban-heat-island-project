using NUnit.Framework;
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
    private Button selectGreeneryZoneButton;
    [SerializeField]
    private Button startGreeneryButton;

    [Header("그늘막 설치 시뮬레이션 탭")]
    [SerializeField]
    private Button installShadeButton;


    // 드롭다운에 들어갈 옵션 리스트
    private List<string> dateOptions = new List<string>();


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
        dateDropdown.value = 0;  // 0번 인덱스를 default로
        dateDropdown.RefreshShownValue();  // 화면 갱신

        // 드롭다운 리스너 연결
        dateDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    // 드롭다운 아이템을 선택했을 때 실행될 기능
    private void OnDropdownValueChanged(int index)
    {
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
        Debug.Log("[ControlPanel] 시각화 시작 버튼 클릭. 서버에 데이터 요청을 보냅니다.");

        // NetworkManager에게 현재 선택된 날짜로 새로고침하도록 요청
        NetworkManager.Instance.RefreshDecalData();
    }

    // 옥상 녹화 구역 선택 버튼을 눌렀을 때 실행될 함수
    private void OnSelectGreeneryZoneButtonClicked()
    {
        // 위에 함수 참고해서 구현
    }
}
