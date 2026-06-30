using System.Collections.Generic;
using UnityEngine;

// Grid GameObject에 붙일 컴포넌트
// 사용 예시
//      => WeatherGridItem clickedGrid = hit.collider.GetComponent<WeatherGridItem>();
public class WeatherGridItem : MonoBehaviour
{
    public ZoneData data;

    public (double lon, double lat) GetCenter()
    {
        if (data?.polygon == null) return (0.0, 0.0);

        int n = data.polygon.GetLength(0);

        // 폴리곤이 닫혀있는지 확인 (첫 점과 마지막 점이 같음)
        if (n > 1 && data.polygon[0, 0] == data.polygon[n - 1, 0] && data.polygon[0, 1] == data.polygon[n - 1, 1])
        {
            n--; // 닫혀있다면 마지막 점은 계산에서 제외
        }

        double totalLon = 0, totalLat = 0;
        for (int i = 0; i < n; i++)
        {
            totalLon += data.polygon[i, 0]; // 경도 합계
            totalLat += data.polygon[i, 1]; // 위도 합계
        }

        return (totalLon / n, totalLat / n);
    }

}
