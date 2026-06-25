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


    void Start()
    {
        // 시작할 때 첫 번째 탭(0번)만 열기
        SwitchTab(0);

        // 버튼 리스너 등록 자동화
        for (int i = 0; i < tabButtons.Length; i++)
        {
            //int index = i; // 클로저(Closure) 문제를 피하기 위해 지역 변수 복사
            tabButtons[i].onClick.AddListener(() => SwitchTab(i));
        }
    }

    public void SwitchTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= contentPanels.Length)
            return;

        for (int i = 0; i < contentPanels.Length; i++)
        {
            // 선택한 인덱스의 패널만 true, 나머지는 false
            contentPanels[i].SetActive(i == tabIndex);

            // 선택된 버튼은 진하게 표시
            if (tabButtons[i].TryGetComponent<Image>(out Image btnImage))
            {
                btnImage.color = Color.whiteSmoke;
            }
        }
    }
}
