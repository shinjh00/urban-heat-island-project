using TMPro;
using UnityEngine;

public class BuildingInfoPanel : MonoBehaviour
{
    [Header("UI Text Components (BuildingData)")]
    [SerializeField] private TMP_Text txtType;          // 용도 (type)
    [SerializeField] private TMP_Text txtMaterial;      // 구조재 (material)
    [SerializeField] private TMP_Text txtAreaSize;      // 면적 (areaSize)
    [SerializeField] private TMP_Text txtFloors;        // 층수 (floors)

    [Header("UI Text Components (Simulation Data)")]
    [SerializeField] private TMP_Text txtGreenerStatus;  // 우선순위상태 (greeneryStatus)
    [SerializeField] private TMP_Text txtGreeneryScoreRank; // 녹화점수 (greeneryScore, greeneryRank)

    /// <summary>
    /// 건물이 선택되었을 때 UIManager에 의해 호출되어 패널을 활성화하고 텍스트를 채웁니다.
    /// </summary>
    public void Show(BuildingInfo info)
    {
        if (info == null || info.data == null) return;

        // 1. 패널 UI 활성화
        gameObject.SetActive(true);

        // 2. BuildingData 원본 데이터 매핑
        if (txtType != null) txtType.text = $"{info.data.type}";
        if (txtMaterial != null) txtMaterial.text = $"{info.data.material}";
        if (txtAreaSize != null) txtAreaSize.text = $"{info.data.areaSize:N1} m²"; // 천단위 쉼표 + 소수점 1자리
        if (txtFloors != null) txtFloors.text = $"{info.data.floors} 층";

        // 3. BuildingInfo 컴포넌트에 주입된 실시간 시뮬레이션 데이터 매핑
        string greeneryGrade = "일반 건물";

        // Enum 상태값(GreeneryState)에 따라 녹화 등급 문자열 매핑
        switch (info.greeneryStatus)
        {
            case GreeneryState.GreeneryPriority:
                greeneryGrade = "우선 녹화 건물";
                break;
            case GreeneryState.GreeneryTop10:
                greeneryGrade = "최우선 녹화 건물";
                break;
            case GreeneryState.Normal:
            default:
                greeneryGrade = "일반 건물";
                break;
        }

        // 시뮬레이션이 한 번도 실행되지 않았거나 랭킹이 아직 산출되지 않은 빌딩에 대한 방어 코드
        if (info.greeneryRank > 0)
        {
            if (txtGreenerStatus != null)
                txtGreenerStatus.text = $"녹화 등급 : {greeneryGrade}";

            if (txtGreeneryScoreRank != null)
                txtGreeneryScoreRank.text = $"녹화 점수 : {info.greeneryScore:F2} 점 ({info.greeneryRank}위)"; // 소수점 2자리 표기
        }
        else
        {
            // 시뮬레이션 결과가 없는 상태 (기본값)
            if (txtGreenerStatus != null)
                txtGreenerStatus.text = "녹화 등급 : 일반 건물";

            if (txtGreeneryScoreRank != null)
                txtGreeneryScoreRank.text = "녹화 점수 : 계산 전";
        }

        Debug.Log($"[BuildingInfoPanel] {info.data.id} 건물 정보 출력 완료. (Status: {info.greeneryStatus}, Score: {info.greeneryScore}, Rank: {info.greeneryRank})");
    }

    /// <summary>
    /// 건물 선택이 해제되었을 때 호출되어 텍스트를 비우고 패널을 끕니다.
    /// </summary>
    public void Hide()
    {
        if (txtType != null) txtType.text = string.Empty;
        if (txtMaterial != null) txtMaterial.text = string.Empty;
        if (txtAreaSize != null) txtAreaSize.text = string.Empty;
        if (txtFloors != null) txtFloors.text = string.Empty;
        if (txtGreenerStatus != null) txtGreenerStatus.text = string.Empty;
        if (txtGreeneryScoreRank != null) txtGreeneryScoreRank.text = string.Empty;

        gameObject.SetActive(false);
    }
}