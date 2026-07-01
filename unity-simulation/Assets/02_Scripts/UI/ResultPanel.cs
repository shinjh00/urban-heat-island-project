using System;
using TMPro;
using UnityEngine;

public class ResultPanel : MonoBehaviour
{
    [Header("TMP UI Elements")]
    [SerializeField] private TextMeshProUGUI GreeningRatio_txt;          // 목표 녹화율 (%)
    [SerializeField] private TextMeshProUGUI GreeningCount_txt; // 녹화 적용된 건물 수 (개)
    [SerializeField] private TextMeshProUGUI TotalGreeningArea_txt;  // 녹화 적용 면적 (㎡)
    [SerializeField] private TextMeshProUGUI ExpectedEffect;    // 예상 감소 온도 (°C)

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

        // 1. 목표 녹화율 (소수점 1자리 표기 예시)
        if (GreeningRatio_txt != null)
            GreeningRatio_txt.text = $"목표 녹화율 : {controller.greeneryRate:F1} %";

        // 2. 녹화 건물 수
        if (GreeningCount_txt != null)
            GreeningCount_txt.text = $"녹화 건물 수: {controller.greeneryBuildingcount} 개";

        // 3. 녹화 적용 면적 (㎡)
        if (TotalGreeningArea_txt != null)
            TotalGreeningArea_txt.text = $"녹화 전용 면적(㎡) : {controller.expectedGreeneryArea:N1} ㎡"; // N1은 천단위 쉼표(,) 추가

        // 4. 예상 감소 온도
        if (ExpectedEffect != null)
            ExpectedEffect.text = $"예상 감소 온도 -{controller.expectedTempEffect:F2} °C" +
                $"녹화 후 예상 온도 : {controller.greeneryAfterTemp:F2} °C";

        Debug.Log("[ResultPanel] 시뮬레이션 결과 UI 갱신이 완료되었습니다.");
    }

    /// <summary>
    /// 필요 시 UI 글자들을 초기화하는 함수
    /// </summary>
    public void ClearDisplay()
    {
        if (GreeningRatio_txt != null) GreeningRatio_txt.text = "- %";
        if (GreeningCount_txt != null) GreeningCount_txt.text = "- 개";
        if (TotalGreeningArea_txt != null) TotalGreeningArea_txt.text = "- ㎡";
        if (ExpectedEffect != null) ExpectedEffect.text = "- °C";
    }
}
