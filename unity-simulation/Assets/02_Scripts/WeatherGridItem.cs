using System.Collections.Generic;
using UnityEngine;

public class WeatherGridItem : MonoBehaviour
{
    // 클릭 시 꺼내 쓸 격자 정보 데이터들
    public int gridId;
    public double temperature;
    public List<double[]> polygonPoints = new List<double[]>();


    public (double lon, double lat) GetCenter()
    {
        var pts = polygonPoints;
        int n = pts.Count;

        // GeoJSON 폴리곤은 첫 점 == 끝 점으로 닫혀있음 → 끝 점 1개는 평균에서 제외
        if (n > 1 && pts[0][0] == pts[n - 1][0] && pts[0][1] == pts[n - 1][1])
            n--;

        double lon = 0, lat = 0;
        for (int i = 0; i < n; i++)
        {
            lon += pts[i][0]; // 경도
            lat += pts[i][1]; // 위도
        }
        return (lon / n, lat / n);
    }


}

/*
 다른곳에서 쓸 때
해당 클래스에 추가
WeatherGridItem clickedGrid = hit.collider.GetComponent<WeatherGridItem>();
*/