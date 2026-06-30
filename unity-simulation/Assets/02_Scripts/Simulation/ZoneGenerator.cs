using CesiumForUnity;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

// 구 영역을 그리드로 분할해서 지도 위에 zone 영역 생성
public class ZoneGenerator : MonoBehaviour
{
    [Header("Cesium References")]
    public CesiumGeoreference georeference;
    public Transform georeferenceTransform;

    [HideInInspector]
    public ZoneRenderer zoneRenderer;

    public List<GameObject> spawnedGrids = new List<GameObject>();


    private void OnEnable()
    {
        ZoneManager.OnRequestGridShow += HandleGridGeneration;
    }

    private void OnDisable()
    {
        ZoneManager.OnRequestGridShow -= HandleGridGeneration;
    }


    // 그리드 생성
    private void HandleGridGeneration()
    {
        Debug.Log("[ZoneGenerator] HandleGridGeneration 시작");

        // 그리드 머티리얼 초기화
        zoneRenderer.InitRuntimeMaterial();

        // ZoneManager의 zoneList를 가져와서 그리드 배치
        List<ZoneData> dataList = ZoneManager.Instance.zoneList;

        foreach (ZoneData grid in dataList)
        {
            StartCoroutine(CreateGrid(grid));
        }
    }

    private IEnumerator CreateGrid(ZoneData grid)
    {
        Debug.Log("[ZoneGenerator] CreateGrid 코루틴 시작");
        // 1. polygon[5, 2] 배열에서 꼭짓점 데이터 추출
        // Cesium 지형 계산을 위해 4개의 꼭짓점 사용 (마지막 5번째는 시작점과 같음)
        double3[] geoPoints = new double3[4];
        for (int i = 0; i < 4; i++)
        {
            geoPoints[i] = new double3(grid.polygon[i, 0], grid.polygon[i, 1], 0.0);
        }

        // 2. 사각형의 중심점 계산
        double avgLon = (geoPoints[0].x + geoPoints[1].x + geoPoints[2].x + geoPoints[3].x) / 4.0;
        double avgLat = (geoPoints[0].y + geoPoints[1].y + geoPoints[2].y + geoPoints[3].y) / 4.0;
        double finalTerrainHeight = 40.0; // 기본 고도

        // 3. Raycast를 통해 지형 높이 감지
        double3 highEcef = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(new double3(avgLon, avgLat, 800.0));
        double3 highUnity = georeference.TransformEarthCenteredEarthFixedPositionToUnity(highEcef);
        Vector3 rayOrigin = new Vector3((float)highUnity.x, (float)highUnity.y, (float)highUnity.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 1500f))
        {
            double3 hitEcef = georeference.TransformUnityPositionToEarthCenteredEarthFixed(new double3(hit.point.x, hit.point.y, hit.point.z));
            double3 hitGeo = CesiumWgs84Ellipsoid.EarthCenteredEarthFixedToLongitudeLatitudeHeight(hitEcef);
            finalTerrainHeight = hitGeo.z + 5.0; // 지면 위 5m 배치
        }

        // 4. Quad 생성 및 배치
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        obj.name = "Zone_" + grid.zoneId;
        obj.transform.SetParent(georeferenceTransform);

        CesiumGlobeAnchor anchor = obj.AddComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = new double3(avgLon, avgLat, finalTerrainHeight);

        // 5. 그리드 크기 계산 (Cesium 좌표간 거리)
        double3 ecef0 = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(geoPoints[0]);
        double3 ecef1 = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(geoPoints[1]);
        double3 ecef3 = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(geoPoints[3]);
        float width = (float)math.distance(ecef0, ecef1);
        float height = (float)math.distance(ecef0, ecef3);
        obj.transform.localScale = new Vector3(width, height, 1f);
        obj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // 6. 데이터 주입: 선택 로직을 위해 컴포넌트에 원본 데이터 저장
        WeatherGridItem item = obj.AddComponent<WeatherGridItem>();
        item.data.zoneId = grid.zoneId;
        item.data.temperature = grid.temperature;
        // 필요시 item.data = grid; 추가
        // 다른 스크립트에서 hit.collider.GetComponent<WeatherGridItem>() 이런식으로 호출해서 사용?

        // 7. 머티리얼 적용
        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        zoneRenderer.ApplyTemperatureStyle(mr, grid.temperature);

        spawnedGrids.Add(obj);
        yield return null;
    }



    //public void VisualizeWeatherGrids(List<ZoneData> weatherGrids, float minTemp, float maxTemp)
    //{
    //    ClearExistingGrids();
    //    foreach (var grid in weatherGrids)
    //    {
    //        if (grid.polygon == null || grid.polygon.Length < 3) continue;
    //        StartCoroutine(BuildWeatherQuadRaycast(grid, minTemp, maxTemp));
    //    }
    //}

    //private IEnumerator BuildWeatherQuadRaycast(ZoneData grid, float min, float max)
    //{
    //    // ... 기존 BuildWeatherQuadRaycast 내부 로직 동일 ...
    //    // (GameObject 생성, CesiumGlobeAnchor 설정, WeatherGridItem 컴포넌트 추가 로직 포함)
    //    // ...

    //    if (geoRef == null || generatedBaseMaterial == null) yield break;

    //    double avgLon = (geoPoints[0].x + geoPoints[1].x + geoPoints[2].x + geoPoints[3].x) / 4.0;
    //    double avgLat = (geoPoints[0].y + geoPoints[1].y + geoPoints[2].y + geoPoints[3].y) / 4.0;

    //    double finalTerrainHeight = 40.0;

    //    double3 highEcef = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(new double3(avgLon, avgLat, 800.0));
    //    double3 highUnity = geoRef.TransformEarthCenteredEarthFixedPositionToUnity(highEcef);
    //    Vector3 rayOrigin = new Vector3((float)highUnity.x, (float)highUnity.y, (float)highUnity.z);

    //    if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 1500f))
    //    {
    //        double3 hitEcef = geoRef.TransformUnityPositionToEarthCenteredEarthFixed(new double3(hit.point.x, hit.point.y, hit.point.z));
    //        double3 hitGeo = CesiumWgs84Ellipsoid.EarthCenteredEarthFixedToLongitudeLatitudeHeight(hitEcef);

    //        // [지면 파묻힘 방지] 수치를 5.0m로 넉넉히 올려 산악/경사 지형 묻힘을 방지합니다.
    //        finalTerrainHeight = hitGeo.z + 5.0;
    //    }
    //    else
    //    {
    //        finalTerrainHeight = 80.0; // 레이저 미스 시 기본 백업 고도 상향
    //    }

    //    double3 centerGeo = new double3(avgLon, avgLat, finalTerrainHeight);

    //    double3 ecef0 = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(geoPoints[0]);
    //    double3 ecef1 = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(geoPoints[1]);
    //    double3 ecef3 = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(geoPoints[3]);


    //    float width = (float)math.distance(ecef0, ecef1);
    //    float height = (float)math.distance(ecef0, ecef3);

    //    GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
    //    obj.name = $"Weather_Grid_ID_{gridId} [{temperature:F1}°C]";
    //    if (georeferenceTransform != null) obj.transform.SetParent(georeferenceTransform);

    //    CesiumGlobeAnchor anchor = obj.AddComponent<CesiumGlobeAnchor>();
    //    anchor.longitudeLatitudeHeight = centerGeo;

    //    obj.transform.localScale = new Vector3(width, height, 1f);
    //    obj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

    //    // [핵심 데이터 주입] 생성된 쿼드 객체에 컴포넌트를 붙이고 원본 데이터를 통째로 이식합니다.
    //    WeatherGridItem itemComponent = obj.AddComponent<WeatherGridItem>();
    //    itemComponent.gridId = gridId;
    //    itemComponent.temperature = temperature;
    //    itemComponent.polygonPoints = new List<double[]>(originalPoints);

    //    spawnedGrids.Add(obj);
    //    ZoneRenderer.Instance.ApplyTemperatureColor(obj.GetComponent<MeshRenderer>(), (float)grid.temperature, min, max);
    //}
    //private IEnumerator BuildWeatherQuadRaycast(double3[] geoPoints, float temperature, int gridId, List<double[]> originalPoints)
    //{
    //    if (geoRef == null || generatedBaseMaterial == null) yield break;

    //    double avgLon = (geoPoints[0].x + geoPoints[1].x + geoPoints[2].x + geoPoints[3].x) / 4.0;
    //    double avgLat = (geoPoints[0].y + geoPoints[1].y + geoPoints[2].y + geoPoints[3].y) / 4.0;

    //    double finalTerrainHeight = 40.0;

    //    double3 highEcef = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(new double3(avgLon, avgLat, 800.0));
    //    double3 highUnity = geoRef.TransformEarthCenteredEarthFixedPositionToUnity(highEcef);
    //    Vector3 rayOrigin = new Vector3((float)highUnity.x, (float)highUnity.y, (float)highUnity.z);

    //    if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 1500f))
    //    {
    //        double3 hitEcef = geoRef.TransformUnityPositionToEarthCenteredEarthFixed(new double3(hit.point.x, hit.point.y, hit.point.z));
    //        double3 hitGeo = CesiumWgs84Ellipsoid.EarthCenteredEarthFixedToLongitudeLatitudeHeight(hitEcef);

    //        // [지면 파묻힘 방지] 수치를 5.0m로 넉넉히 올려 산악/경사 지형 묻힘을 방지합니다.
    //        finalTerrainHeight = hitGeo.z + 5.0;
    //    }
    //    else
    //    {
    //        finalTerrainHeight = 80.0; // 레이저 미스 시 기본 백업 고도 상향
    //    }

    //    double3 centerGeo = new double3(avgLon, avgLat, finalTerrainHeight);

    //    double3 ecef0 = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(geoPoints[0]);
    //    double3 ecef1 = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(geoPoints[1]);
    //    double3 ecef3 = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(geoPoints[3]);


    //    float width = (float)math.distance(ecef0, ecef1);
    //    float height = (float)math.distance(ecef0, ecef3);

    //    GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
    //    obj.name = $"Weather_Grid_ID_{gridId} [{temperature:F1}°C]";
    //    if (georeferenceTransform != null) obj.transform.SetParent(georeferenceTransform);

    //    CesiumGlobeAnchor anchor = obj.AddComponent<CesiumGlobeAnchor>();
    //    anchor.longitudeLatitudeHeight = centerGeo;

    //    obj.transform.localScale = new Vector3(width, height, 1f);
    //    obj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

    //    // [핵심 데이터 주입] 생성된 쿼드 객체에 컴포넌트를 붙이고 원본 데이터를 통째로 이식합니다.
    //    WeatherGridItem itemComponent = obj.AddComponent<WeatherGridItem>();
    //    itemComponent.gridId = gridId;
    //    itemComponent.temperature = temperature;
    //    itemComponent.polygonPoints = new List<double[]>(originalPoints);

    //    spawnedGrids.Add(obj);

    //    MeshRenderer mr = obj.GetComponent<MeshRenderer>();
    //    if (mr != null)
    //    {
    //        Material uniqueMat = Instantiate(generatedBaseMaterial);
    //        Color targetColor = GetColorFromTemperature(temperature);
    //        targetColor.a = 0.45f;

    //        if (uniqueMat.HasProperty("_BaseColor")) uniqueMat.SetColor("_BaseColor", targetColor);
    //        else uniqueMat.SetColor("_Color", targetColor);

    //        mr.material = uniqueMat;
    //        generatedMaterials.Add(uniqueMat);
    //    }
    //}





    private void ClearExistingGrids()
    {
        foreach (var obj in spawnedGrids) if (obj != null) Destroy(obj);
        spawnedGrids.Clear();
    }
}
