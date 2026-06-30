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

    private List<string> dateOptions = new List<string>();

    [Header("옥상 녹화 시뮬레이션 탭")]
    [SerializeField]
    private Button selectBuildingButton;
    [SerializeField]
    private Button startGreeneryButton;

    [Header("그늘막 설치 시뮬레이션 탭")]
    [SerializeField]
    private Button installShadeButton;




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
    }

    // 드롭다운 아이템을 선택했을 때 실행될 기능
    // UIManager에 보내서 다른 스크립트에서 UIManager 싱글톤으로 접근할수있게?
    private void OnDropdownValueChanged(int index)
    {
        // dateDropdown.options[index].text 를 하면 유저가 선택한 "2026년 06월" 같은 글자를 가져올 수 있습니다.
        Debug.Log("선택된 날짜: " + dateDropdown.options[index].text);
    }
}
