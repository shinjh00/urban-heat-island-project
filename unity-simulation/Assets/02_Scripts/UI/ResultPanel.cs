using TMPro;
using UnityEngine;

public class ResultPanel : MonoBehaviour
{
    [Header("TMP UI Elements")]
    [SerializeField] private TMP_Text GreeningRatio_txt;        // 녹화율 : 75%
    [SerializeField] private TMP_Text GreeningCount_txt;        // 녹화 건물 개수 : 263개
    [SerializeField] private TMP_Text TotalGreeningArea_txt;    // 녹화 적용 면적: (㎡) 총 18,750m
    [SerializeField] private TMP_Text ExpectedEffect;           // 예상 효과: 15.7C → 15.54C (0.16C 감소)

    private void Start()
    {
        // [구독] 컨트롤러에서 데이터 연산 및 동기화가 끝나면 UI를 갱신하도록 매핑
        SimulationController.OnResultDataUpdated += UpdateResultDisplay;

        // 게임 시작 시점에는 패널을 숨기거나 초기화하고 싶다면 대입 (선택 사항)
        // ClearDisplay();
    }

    private void OnDestroy()
    {
        //[구독 해제] 메모리 누수 방지
        SimulationController.OnResultDataUpdated -= UpdateResultDisplay;
    }

    /// <summary>
    /// SimulationController의 정적 인스턴스로부터 최신 데이터를 가져와 텍스트를 갱신합니다.
    /// </summary>
    private void UpdateResultDisplay()
    {
        // 싱글톤 인스턴스 안전성 체크
        var controller = SimulationController.Instance;
        if (controller == null) return;

        // 1. 녹화율
        int greeneryRate = (int)(controller.greeneryRate * 100);
        if (GreeningRatio_txt != null)

            GreeningRatio_txt.text = $"{greeneryRate} %";

        // 2. 녹화 건물 개수
        if (GreeningCount_txt != null)
            GreeningCount_txt.text = $"{controller.greeneryBuildingcount} 개";

        // 3. 녹화 적용 면적 (㎡)
        if (TotalGreeningArea_txt != null)
            TotalGreeningArea_txt.text = $"총 {controller.expectedGreeneryArea:#,##0.00} m²";

        // 4. 예상 효과
        if (ExpectedEffect != null)
            ExpectedEffect.text = $"{controller.gridOriginalTemp:F2}°C -> {controller.greeneryAfterTemp:F2}°C\n({controller.expectedTempEffect:F2}°C 감소)";

        UIManager.Instance.ShowResultPanel();

        Debug.Log("[ResultPanel] 시뮬레이션 결과 UI 갱신이 완료되었습니다.");
    }

    /// <summary>
    /// 필요 시 UI 글자들을 초기화하는 함수
    /// </summary>
    public void ClearDisplay()
    {
        if (GreeningCount_txt != null) GreeningCount_txt.text = "- 개";
        if (TotalGreeningArea_txt != null) TotalGreeningArea_txt.text = "- ㎡";
        if (ExpectedEffect != null) ExpectedEffect.text = "- °C";
    }
}
