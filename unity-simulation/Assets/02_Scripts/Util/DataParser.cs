using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

//JObject -> { } 중괄호(딕셔너리 형태)
//JArray  -> [ ] 대괄호(배열 형태)
//JToken  -> 둘 다 포함하는 부모 타입

// Json 파일 파싱 스크립트
public class DataParser
{
    private const double LAT_SCALE = 111320.0;
    private const double MIN_POLYGON_SIZE_METERS = 2.0;
    private const float DEFAULT_HEIGHT = 3.0f;
    private const float METERS_PER_FLOOR = 3.0f;


    // GeoJSON 파싱 — StreamingAssets의 GeoJSON 파일 처리
    public async Task<List<JObject>> ParseGeoJson(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

        if (!File.Exists(path))
        {
            Debug.LogError("[DataParser] 파일 경로에 없음: " + path);
            return new List<JObject>();
        }

        string json = await Task.Run(() => File.ReadAllText(path));

        return await Task.Run(() =>
        {
            List<JObject> features = new List<JObject>();
            JObject root = JObject.Parse(json);
            foreach (JToken feat in (JArray)root["features"])
            {
                features.Add((JObject)feat);
            }
            return features;
        });
    }


    // JSON 파싱 — StreamingAssets의 JSON 파일 처리
    // 사용 예시:  List<District> __ = await parser.ParseJson<District>("seoul_districts.json");
    public async Task<List<T>> ParseJson<T>(string fileName)
    {
        List<T> result = new List<T>();

        string path = Path.Combine(Application.streamingAssetsPath, fileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"[DataParser] 파일 없음: {path}");
            return result;
        }

        string json = File.ReadAllText(path);

        result = await Task.Run(() =>
            JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>()
        );

        Debug.Log($"[DataParser] {fileName} 로드 완료 — {result.Count}개");
        return result;
    }


    #region ``각 Data 추출 함수``
    // BuildingData 파싱 및 객체화
    public BuildingData ExtractBuildingData(JObject feat)
    {
        try
        {
            JToken props = feat["properties"];
            JToken geom = feat["geometry"];
            if (props == null || geom == null) return null;
            if (geom["type"]?.ToString() != "Polygon") return null;

            JArray ring = (JArray)geom["coordinates"][0];
            if (ring == null || ring.Count < 3) return null;

            List<double[]> rawCoords = new List<double[]>();

            foreach (JArray point in ring)
            {   // 주의: GeoJSON 은 경도(x), 위도(y) 순서
                // point[0] - 경도, point[1] - 위도
                double lon = (double)point[0]; // 경도
                double lat = (double)point[1]; // 위도

                // 경도, 위도를 배열로 묶어서 리스트에 추가
                double[] coord = new double[] { lon, lat };
                rawCoords.Add(coord);
            }

            if (!IsValidPolygon(rawCoords)) return null;

            BuildingData data = new BuildingData();

            data.id = props["원천도형ID"]?.ToString();
            data.dongCode = props["행정동코드"]?.ToString();
            data.address = props["주소"]?.ToString();
            data.addressDetail = props["지번"]?.ToString();
            data.type = props["용도"]?.ToString();
            data.material = props["구조재"]?.ToString();
            data.areaSize = float.Parse(props["면적"]?.ToString() ?? "0.0");
            data.height = float.Parse(props["높이"]?.ToString() ?? "0.0");
            data.floors = int.Parse(props["층수"]?.ToString() ?? "0");

            double floors = 0;
            if (props["층수"] != null && props["층수"].Type != JTokenType.Null)
                floors = (double)props["층수"];
            double preHeight = 0;
            if (props["높이"] != null && props["높이"].Type != JTokenType.Null)
                preHeight = (double)props["높이"];
            if (preHeight > 0 && floors > 0)
            {
                double meterPerFloor = preHeight / floors;
                if (meterPerFloor < 2.0 || meterPerFloor > 10.0)
                {
                    preHeight = 0;
                }
            }
            //높이 계산
            if (preHeight > 0)
                data.height = (float)preHeight;
            else if (floors > 0)
                data.height = (float)(floors * METERS_PER_FLOOR);
            else
                data.height = DEFAULT_HEIGHT;
            data.floors = (int)floors;

            double lonSum = 0;
            double latSum = 0;

            foreach (double[] c in rawCoords)
            {
                lonSum += c[0]; // 경도 합산
                latSum += c[1]; // 위도 합산
            }

            data.lon = lonSum / rawCoords.Count; // 경도 평균
            data.lat = latSum / rawCoords.Count; // 위도 평균

            data.polygon = ConvertToLocalPolygon(rawCoords, data.lon, data.lat);

            if (data.polygon.Count > 1 &&
                data.polygon[0] == data.polygon[data.polygon.Count - 1])
                data.polygon.RemoveAt(data.polygon.Count - 1);

            if (data.polygon.Count < 3) return null;

            return data;
        }
        catch { return null; }
    }

    // ZoneData 파싱 및 객체화
    public List<ZoneData> ExtractZoneData(List<JObject> features)
    {
        List<ZoneData> zoneList = new List<ZoneData>();
        int index = 0;  // zoneid 임시 부여를 위한 변수 (나중에 삭제)

        foreach (JObject feat in features)
        {
            try
            {
                JToken props = feat["properties"];
                JToken geom = feat["geometry"];

                if (props == null || geom == null) continue;
                if (geom["type"]?.ToString() != "Polygon") continue;

                // Polygon 좌표 추출 (Vector2 배열로 변환)
                JArray grid = (JArray)geom["coordinates"][0];
                int pointCount = grid.Count;
                if (grid == null || grid.Count < 3) return null;

                // polygon 2차원 배열로 생성
                double[,] polygonArray = new double[pointCount, 2];
                for (int i = 0; i < pointCount; i++)
                {
                    polygonArray[i, 0] = (double)grid[i][0]; // 경도
                    polygonArray[i, 1] = (double)grid[i][1]; // 위도
                }

                // ZoneData 객체 생성
                ZoneData zone = new ZoneData
                {
                    // zoneId가 GeoJSON에 있다면 가져오고, 없으면 임의 생성
                    zoneId = props["zoneId"]?.ToString() ?? $"ZONE_{index}",
                    temperature = (float)(props["value"] ?? 0.0f),
                    polygon = polygonArray
                };

                zoneList.Add(zone);
                index++;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DataParser] Grid 데이터 파싱 오류: {e.Message}");
                continue;
            }
        }
        return zoneList;
    }
    #endregion


    #region ``Polygon 계산 함수``
    private List<Vector2> ConvertToLocalPolygon(List<double[]> rawCoords, double cLon, double cLat)
    {
        double lonScale = LAT_SCALE * Math.Cos(cLat * Math.PI / 180.0);

        List<Vector2> polygon = new List<Vector2>();
        foreach (double[] c in rawCoords)
        {
            // 중심점에서 얼마나 떨어져 있는지 미터로 계산한다.
            float x = (float)((c[0] - cLon) * lonScale);
            float z = (float)((c[1] - cLat) * LAT_SCALE);
            polygon.Add(new Vector2(x, z));
        }
        return polygon;
    }
    private bool IsValidPolygon(List<double[]> coords)
    {
        if (coords.Count < 3) return false;

        double cLat = 0;
        double minLon = double.MaxValue, maxLon = double.MinValue;
        double minLat = double.MaxValue, maxLat = double.MinValue;

        foreach (double[] c in coords)
        {
            cLat += c[1];
            if (c[0] < minLon) minLon = c[0];
            if (c[0] > maxLon) maxLon = c[0];
            if (c[1] < minLat) minLat = c[1];
            if (c[1] > maxLat) maxLat = c[1];
        }

        // 위도 평균값 계산 (경도 1도당 미터 거리 보정에 사용)
        cLat /= coords.Count;

        // 도 단위 차이를 미터로 변환하여 건물의 가로/세로 크기를 구한다.
        double lonScale = LAT_SCALE * Math.Cos(cLat * Math.PI / 180.0);
        double xRange = (maxLon - minLon) * lonScale;
        double zRange = (maxLat - minLat) * LAT_SCALE;

        // 가로 세로 중 긴 쪽이 2m 미만이면 실제로 존재할 수 없는 건물이므로 탈락
        return Math.Max(xRange, zRange) >= MIN_POLYGON_SIZE_METERS;
    }
    #endregion
}