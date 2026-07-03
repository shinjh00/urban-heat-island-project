using UnityEngine;
using TMPro;
using System.Linq;

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

        // 3. SimulationController 인스턴스의 실시간 랭킹 리스트 조회 후 매핑
        if (SimulationController.Instance != null && SimulationController.Instance.greeneryRankingBuildings != null)
        {
            // 리스트에서 현재 건물 고유 ID와 일치하는 시뮬레이션 결과 추출
            var simData = SimulationController.Instance.greeneryRankingBuildings
                .FirstOrDefault(b => b.buildingID == info.data.id);

            // 데이터가 존재할 경우 (구조체의 기본값 체크: string이 비어있지 않다면)
            if (!string.IsNullOrEmpty(simData.buildingID))
            {
                string greeneryGrade = "일반 건물"; // greeneryStatus 에 따른 상태 구분 "일반 건물", "우선 녹화 건물", "최우선 녹화 건물"

                if(simData.greeneryStatus == "GreeneryPriority")
                {
                    greeneryGrade = "우선 녹화 건물";
                }else if(simData.greeneryStatus == "GreeneryTop10")
                {
                    greeneryGrade = "최우선 녹화 건물";
                }
                else
                {
                    greeneryGrade = "일반 건물";
                }

                if (txtGreenerStatus != null)
                    txtGreenerStatus.text = $"녹화 등급 : {greeneryGrade}";

                if (txtGreeneryScoreRank != null)
                    txtGreeneryScoreRank.text = $"종합 점수 : {simData.greeneryScore:F2} 점 ({simData.greeneryRank}위)"; // 소수점 2자리까지만 깔끔하게 표기
            }
            else
            {
                // 이번 시뮬레이션 연산 구역에 포함되지 않은 건물인 경우 방어 코드
                if (txtGreenerStatus != null) txtGreenerStatus.text = "녹화등급 : 대상 아님";
                if (txtGreeneryScoreRank != null) txtGreeneryScoreRank.text = "녹화 점수 : -";
            }
        }
        else
        {
            // 시뮬레이션 데이터 자체가 아직 존재하지 않는 단계인 경우
            if (txtGreenerStatus != null) txtGreenerStatus.text = "녹화 등급 : 일반 건물";
            if (txtGreeneryScoreRank != null) txtGreeneryScoreRank.text = "녹화 점수 : 계산 전";
        }

        Debug.Log($"[BuildingInfoPanel] {info.data.id} 건물 정보 출력 완료.");
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