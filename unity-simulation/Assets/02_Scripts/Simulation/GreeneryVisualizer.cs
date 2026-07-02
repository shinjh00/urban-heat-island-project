using UnityEngine;

// 시뮬레이션 결과(greeneryStatus 문자열)에 따라 건물 색상을 입히는 클래스
// SimulationController.OnResultDataUpdated 이벤트를 구독해서 동작함
public class GreeneryVisualizer : MonoBehaviour
{
    [Header("녹화 상태 색상")]
    [SerializeField] private Color top10Color = new Color(0.0f, 0.35f, 0.1f);      // Top10 → 진한 초록
    [SerializeField] private Color priorityColor = new Color(0.55f, 0.8f, 0.55f);  // 우선녹화건물 → 연한 초록

    private void OnEnable()
    {
        // Controller가 결과 데이터 세팅을 끝냈을 때 발행하는 이벤트 구독
        SimulationController.OnResultDataUpdated += ApplyGreeneryColors;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위한 구독 해제
        SimulationController.OnResultDataUpdated -= ApplyGreeneryColors;
    }

    // 랭킹 리스트를 순회하며 greeneryStatus 값에 맞는 색을 건물에 적용
    private void ApplyGreeneryColors()
    {
        var ranked = SimulationController.Instance.greeneryRankingBuildings;
        if (ranked == null || ranked.Count == 0)
        {
            Debug.LogWarning("[GreeneryVisualizer] 랭킹 데이터가 비어있습니다.");
            return;
        }

        int coloredCount = 0;

        foreach (var b in ranked)
        {
            // buildingID로 씬에 스폰된 실제 건물 GameObject 찾기
            GameObject obj = BuildingManager.Instance.GetBuilding(b.buildingID);
            if (obj == null) continue;

            // 건물 클릭 시 정보패널에 표시할 수 있도록 BuildingInfo에 결과 기록
            BuildingInfo info = obj.GetComponent<BuildingInfo>();
            if (info != null)
            {
                info.greeneryScore = b.greeneryScore;
                info.greeneryRank = b.greeneryRank;
            }

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            if (mr == null) continue;

            // greeneryStatus 문자열 값에 따라 색상 분기
            switch (b.greeneryStatus)
            {
                case "GreeneryTop10":
                    mr.material.color = top10Color;
                    coloredCount++;
                    break;

                case "GreeneryPriority":
                    mr.material.color = priorityColor;
                    coloredCount++;
                    break;

                    // "Normal"은 건드리지 않음 (기존 색 유지)
            }
        }

        Debug.Log($"[GreeneryVisualizer] 녹화 색상 적용 완료 — {coloredCount}개");
    }
}