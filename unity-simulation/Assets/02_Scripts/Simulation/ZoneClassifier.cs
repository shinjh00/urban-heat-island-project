using System.Collections.Generic;
using UnityEngine;

// 그리드 범위 체크 연산 및 건물 정보
// 그리드 범위 내에 포함된 건물의 건물 정보에 zoneId 값을 할당하기 위해
public class ZoneClassifier : MonoBehaviour
{

    // 그리드 선택 시 건물 스폰 후 스폰된 건물들에 zoneId, temperature 부여
    public void ClassifyAndApplyZone(string zoneId, double[,] zonePolygon, float temp, Dictionary<string, GameObject> activeBuildings)
    {
        if (activeBuildings == null || activeBuildings.Count == 0) return;

        int mappedCount = 0;

        // 스폰된 모든 건물 오브젝트 순회
        foreach (var kvp in activeBuildings)
        {
            GameObject buildingObj = kvp.Value;
            if (buildingObj == null) continue;

            // 건물의 컴포넌트 데이터 추출
            BuildingInfo info = buildingObj.GetComponent<BuildingInfo>();
            if (info == null || info.data == null) continue;

            // 건물이 그리드 범위 내에 있는지 좌표 연산
            if (IsBuildingInZone(info.data.lon, info.data.lat, zonePolygon))
            {
                info.zoneId = zoneId;
                info.temperature = temp;
                mappedCount++;
            }
        }

        Debug.Log($"[ZoneClassifier] 연산 완료 : 총 {activeBuildings.Count}개 건물 중 {mappedCount}개의 건물을 영역 '{zoneId}'에 성공적으로 매핑했습니다.");
    }


    // 건물의 중심점이 그리드 내부에 포함되는지 판별하는 PIP(Point In Polygon) 알고리즘
    public bool IsBuildingInZone(double bLon, double bLat, double[,] zonePolygon)
    {
        if (zonePolygon == null) return false;

        int numPoints = zonePolygon.GetLength(0);  // 좌표쌍의 총 개수 가져옴 (5개)
        bool inside = false;

        // 다각형 점들을 순회하며 위경도 레이캐스트 연산 수행
        // 1바퀴) i=0, j=4 부터 시작
        for (int i = 0, j = numPoints - 1; i < numPoints; j = i++)
        {
            double xi = zonePolygon[i, 0]; // Longitude (경도 X)
            double yi = zonePolygon[i, 1]; // Latitude (위도 Y)
            double xj = zonePolygon[j, 0];
            double yj = zonePolygon[j, 1];

            // 점이 다각형 변의 경계 내에 있고 변과 교차하는지 검증
            bool intersect = ((yi > bLat) != (yj > bLat))
                && (bLon < (xj - xi) * (bLat - yi) / (yj - yi) + xi);

            if (intersect) inside = !inside;
        }

        return inside;
    }

}
