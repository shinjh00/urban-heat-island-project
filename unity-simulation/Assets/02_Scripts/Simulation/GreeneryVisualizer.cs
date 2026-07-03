using System.Collections.Generic;  // [추가]
using TMPro;                       // [추가]
using UnityEngine;

// 시뮬레이션 결과(greeneryStatus 문자열)에 따라 건물 색상을 입히는 클래스
// SimulationController.OnResultDataUpdated 이벤트를 구독해서 동작함
public class GreeneryVisualizer : MonoBehaviour
{
    [Header("녹화 상태 색상")]
    [SerializeField] private Color top10Color = new Color(0.0f, 0.35f, 0.1f);      // Top10 → 진한 초록
    [SerializeField] private Color priorityColor = new Color(0.55f, 0.8f, 0.55f);  // 우선녹화건물 → 연한 초록

    [Header("Top10 플로팅 라벨")]
    [SerializeField] private float labelHeightOffset = 20f;   // 지붕 위 여유 높이(m)
    [SerializeField] private float labelFontSize = 72f;
    [SerializeField] private Color labelColor = Color.yellow;
    [SerializeField] private Sprite tooltipSprite;             // 말풍선 스프라이트 드래그
    [SerializeField] private Vector2 bubbleScale = new Vector2(10f, 10f);
    [SerializeField] private float bubbleYOffset = 0f;         // 텍스트-말풍선 미세조정

    // 생성된 라벨 추적용 (재시뮬레이션 시 정리)
    private readonly List<GameObject> spawnedLabels = new List<GameObject>();

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
        ClearLabels();  // 이전 시뮬 라벨부터 정리
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

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            if (mr == null) continue;

            // [구조 개선] greeneryStatus 문자열 값 대신 깔끔한 GreeneryState Enum 기준으로 분기 처리
            switch (b.greeneryStatus)
            {
                case GreeneryState.GreeneryTop10:
                    mr.material.color = top10Color;
                    SpawnLabel(obj, info, b);   // Top10 전용 지붕 위 플로팅 라벨 생성
                    coloredCount++;
                    break;

                case GreeneryState.GreeneryPriority:
                    mr.material.color = priorityColor;
                    coloredCount++;
                    break;

                case GreeneryState.Normal:
                default:
                    // "Normal" 상태는 가공하지 않음 (건물의 원래 기본 색상 유지)
                    break;
            }
        }

        Debug.Log($"[GreeneryVisualizer] 녹화 색상 적용 완료 — {coloredCount}개");
    }

    // Top10 건물 지붕 위에 말풍선 + 순위/점수 라벨 생성
    private void SpawnLabel(GameObject buildingObj, BuildingInfo info, SimulationController.GreeneryRankingBuildings b)
    {
        float roofHeight = (info != null && info.data != null) ? info.data.height : 10f;

        // 1) 순위/점수 텍스트 (3D TMP — Canvas 불필요)
        GameObject labelObj = new GameObject($"Top10Label_{b.greeneryRank}");
        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = $"<size=60%>Rank</size> <color=#1E8449><b>{b.greeneryRank}</b></color>\n" +
            $"<size=60%>Score</size> <b>{b.greeneryScore:F1}</b>";
        tmp.fontSize = labelFontSize;
        tmp.color = labelColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(100f, 40f);  // 잘림 방지
        tmp.GetComponent<MeshRenderer>().sortingOrder = 10;  // [추가] 항상 말풍선 위에


        // [추가] 순위 기반 크기 가중치 (1위 = 1.3배, 10위 = 0.8배)
        float w = 1f - (b.greeneryRank - 1) / 9f;
        labelObj.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.3f, w);


        // 2) 말풍선 배경 (텍스트 뒤에 깔림)
        if (tooltipSprite != null)
        {
            GameObject bubble = new GameObject("Bubble");
            SpriteRenderer sr = bubble.AddComponent<SpriteRenderer>();
            sr.sprite = tooltipSprite;
            sr.flipY = true;  // ★ 꼬리 위 → 아래 (회전 대신 이 한 줄)
            sr.sortingOrder = 9;   // [추가] 항상 텍스트 아래에

            bubble.transform.SetParent(labelObj.transform, false);
            bubble.transform.localPosition = new Vector3(0f, bubbleYOffset, 0.5f);  // +Z = 텍스트보다 뒤
            bubble.transform.localScale = new Vector3(bubbleScale.x, bubbleScale.y, 1f);
        }

        // 3) 건물에 부착 + 항상 카메라를 향하게
        labelObj.transform.SetParent(buildingObj.transform, false);
        labelObj.transform.localPosition = Vector3.up * (roofHeight + labelHeightOffset);
        labelObj.AddComponent<Billboard>();

        spawnedLabels.Add(labelObj);
    }

    // 이전 시뮬레이션에서 생성한 라벨 전부 제거
    private void ClearLabels()
    {
        foreach (GameObject label in spawnedLabels)
            if (label != null) Destroy(label);
        spawnedLabels.Clear();
    }




}