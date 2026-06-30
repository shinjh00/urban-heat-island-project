using CesiumForUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem; // New Input System 패키지 대응

public class WeatherGridVisualizer : MonoBehaviour
{
    private Coroutine blinkRoutine;

    [Header("Cesium References")]
    public CesiumGeoreference geoRef;
    public Transform georeferenceTransform;

    [Header("Dynamic Range (Auto Setup)")]
    private double dynamicMinTemp = -30.0f;
    private double dynamicMaxTemp = 50.0f;

    private Material generatedBaseMaterial;
    private List<GameObject> spawnedGrids = new List<GameObject>();
    private List<Material> generatedMaterials = new List<Material>();

    void Start()
    {
        if (geoRef == null) geoRef = FindFirstObjectByType<CesiumGeoreference>();
        if (georeferenceTransform == null && geoRef != null) georeferenceTransform = geoRef.transform;

        InitRuntimeMaterial();
    }

    void Update()
    {
        // 마우스 왼쪽 버튼 클릭 감지
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                WeatherGridItem gridItem = hit.collider.GetComponent<WeatherGridItem>();
                if (gridItem != null)
                {
                    Debug.Log($" [격자 선택] ID: {gridItem.gridId}번만 표시합니다.");
                    Debug.Log($" [격자 선택] 온도: {gridItem.temperature}");
                    for (int i = 0; i < gridItem.polygonPoints.Count; i++)
                    {
                        double lon = gridItem.polygonPoints[i][0];
                        double lat = gridItem.polygonPoints[i][1];
                        Debug.Log($" 꼭짓점 [{i + 1}] -> [ 경도: {lon:F8}, 위도: {lat:F8} ]");
                    }

                    //  모든 격자를 일단 다 숨깁니다.
                    foreach (var grid in spawnedGrids)
                    {
                        if (grid != null) grid.GetComponent<MeshRenderer>().enabled = false;
                    }

                    //  클릭한 이 격자 하나만 켭니다.
                    MeshRenderer selectedMr = hit.collider.GetComponent<MeshRenderer>();
                    selectedMr.enabled = true;

                    //  추가: Blink → 중심계산 → 건물 로드
                    if (blinkRoutine != null) StopCoroutine(blinkRoutine);
                    blinkRoutine = StartCoroutine(BlinkAndLoad(gridItem, selectedMr));
                }
                else
                {
                    // 격자가 아닌 일반 지면이나 허공을 클릭하면 모든 격자를 다시 숨깁니다.
                    foreach (var grid in spawnedGrids)
                    {
                        if (grid != null) grid.GetComponent<MeshRenderer>().enabled = false;
                    }
                }
            }
        }
    }


    private IEnumerator BlinkAndLoad(WeatherGridItem grid, MeshRenderer mr)
    {
        // 1️. Blink (2초간 0.2초 간격)
        float duration = 2f;
        float interval = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            mr.enabled = !mr.enabled;
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        mr.enabled = true;  // 끝나면 켜진 상태로

        // 2️. 중심점 계산
        var (lon, lat) = grid.GetCenter();
        Debug.Log($"[BlinkAndLoad] 중심점 계산 완료 — ({lon:F6}, {lat:F6})");

        // 3️. 750m 건물 로드
        BuildingManager.Instance.FocusOnGrid(lon, lat);

        // 4. 격자 ID(string)와 온도 데이터(double)를 함께 컨트롤러에 주입하며 호출합니다.
        if (SimulationController.Instance != null)
        {
            SimulationController.Instance.ProcessBuildingGreeneryRankings(
                grid.gridId.ToString(),
                grid.temperature
            );
        }
        else
        {
            Debug.LogError("[WeatherGridVisualizer] 씬에 SimulationController 인스턴스가 존재하지 않습니다.");
        }
    }


    private void InitRuntimeMaterial()
    {
        Shader weatherShader = Shader.Find("Universal Render Pipeline/Lit");
        bool isURP = true;

        if (weatherShader == null)
        {
            weatherShader = Shader.Find("Standard");
            isURP = false;
        }

        generatedBaseMaterial = new Material(weatherShader);
        generatedBaseMaterial.name = "Runtime_WeatherGrid_BaseMat";

        if (isURP)
        {
            generatedBaseMaterial.SetFloat("_Surface", 1);
            generatedBaseMaterial.SetFloat("_Blend", 0);
            generatedBaseMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            generatedBaseMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            generatedBaseMaterial.SetInt("_ZWrite", 0);
            generatedBaseMaterial.DisableKeyword("_ALPHATEST_ON");
            generatedBaseMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            generatedBaseMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            generatedBaseMaterial.SetFloat("_Mode", 3);
            generatedBaseMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            generatedBaseMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            generatedBaseMaterial.SetInt("_ZWrite", 0);
            generatedBaseMaterial.DisableKeyword("_ALPHATEST_ON");
            generatedBaseMaterial.EnableKeyword("_ALPHABLEND_ON");
            generatedBaseMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }

    public void VisualizeWeatherGrids()
    {
        if (api_test.Instance == null || !api_test.Instance.IsGeoJsonLoaded)
        {
            Debug.LogWarning("[시각화 실패] GeoJSON 데이터가 아직 로드되지 않았습니다.");
            return;
        }

        var weatherGrids = api_test.Instance.CachedWeatherGrids;
        if (weatherGrids == null || weatherGrids.Count == 0) return;

        ClearExistingGrids();

        float currentMin = float.MaxValue;
        float floatMax = float.MinValue;
        foreach (var grid in weatherGrids)
        {
            float val = (float)grid.temperature;
            if (val < currentMin) currentMin = val;
            if (val > floatMax) floatMax = val;
        }
        bool isApprox = Mathf.Approximately(currentMin, floatMax);
        dynamicMinTemp = isApprox ? currentMin - 0.5f : currentMin;
        dynamicMaxTemp = isApprox ? floatMax + 0.5f : floatMax;

        foreach (var grid in weatherGrids)
        {
            if (grid.polygonPoints == null || grid.polygonPoints.Count < 4) continue;

            double3[] geoPoints = new double3[4];
            for (int i = 0; i < 4; i++)
            {
                geoPoints[i] = new double3(grid.polygonPoints[i][0], grid.polygonPoints[i][1], 0.0);
            }

            // 생성 함수로 원본 grid 데이터 보따리를 함께 넘겨줍니다.
            StartCoroutine(BuildWeatherQuadRaycast(geoPoints, (float)grid.temperature, grid.id, grid.polygonPoints));
        }

        Debug.Log($" [3/3 완료] 세슘 지면 위에 클릭 가능한 격자 {weatherGrids.Count}개 배치 완료.");
    }

    private IEnumerator BuildWeatherQuadRaycast(double3[] geoPoints, float temperature, int gridId, List<double[]> originalPoints)
    {
        if (geoRef == null || generatedBaseMaterial == null) yield break;

        double avgLon = (geoPoints[0].x + geoPoints[1].x + geoPoints[2].x + geoPoints[3].x) / 4.0;
        double avgLat = (geoPoints[0].y + geoPoints[1].y + geoPoints[2].y + geoPoints[3].y) / 4.0;

        double finalTerrainHeight = 40.0;

        double3 highEcef = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(new double3(avgLon, avgLat, 800.0));
        double3 highUnity = geoRef.TransformEarthCenteredEarthFixedPositionToUnity(highEcef);
        Vector3 rayOrigin = new Vector3((float)highUnity.x, (float)highUnity.y, (float)highUnity.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 1500f))
        {
            double3 hitEcef = geoRef.TransformUnityPositionToEarthCenteredEarthFixed(new double3(hit.point.x, hit.point.y, hit.point.z));
            double3 hitGeo = CesiumWgs84Ellipsoid.EarthCenteredEarthFixedToLongitudeLatitudeHeight(hitEcef);

            // [지면 파묻힘 방지] 수치를 5.0m로 넉넉히 올려 산악/경사 지형 묻힘을 방지합니다.
            finalTerrainHeight = hitGeo.z + 5.0;
        }
        else
        {
            finalTerrainHeight = 80.0; // 레이저 미스 시 기본 백업 고도 상향
        }

        double3 centerGeo = new double3(avgLon, avgLat, finalTerrainHeight);

        double3 ecef0 = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(geoPoints[0]);
        double3 ecef1 = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(geoPoints[1]);
        double3 ecef3 = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(geoPoints[3]);


        float width = (float)math.distance(ecef0, ecef1);
        float height = (float)math.distance(ecef0, ecef3);

        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        obj.name = $"Weather_Grid_ID_{gridId} [{temperature:F1}°C]";
        if (georeferenceTransform != null) obj.transform.SetParent(georeferenceTransform);

        CesiumGlobeAnchor anchor = obj.AddComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = centerGeo;

        obj.transform.localScale = new Vector3(width, height, 1f);
        obj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // [핵심 데이터 주입] 생성된 쿼드 객체에 컴포넌트를 붙이고 원본 데이터를 통째로 이식합니다.
        WeatherGridItem itemComponent = obj.AddComponent<WeatherGridItem>();
        itemComponent.gridId = gridId;
        itemComponent.temperature = temperature;
        itemComponent.polygonPoints = new List<double[]>(originalPoints);

        spawnedGrids.Add(obj);

        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Material uniqueMat = Instantiate(generatedBaseMaterial);
            Color targetColor = GetColorFromTemperature(temperature);
            targetColor.a = 0.45f;

            if (uniqueMat.HasProperty("_BaseColor")) uniqueMat.SetColor("_BaseColor", targetColor);
            else uniqueMat.SetColor("_Color", targetColor);

            mr.material = uniqueMat;
            generatedMaterials.Add(uniqueMat);
        }
    }

    private Color GetColorFromTemperature(double temp)
    {
        double t = Mathf.InverseLerp((float)dynamicMinTemp, (float)dynamicMaxTemp, (float)temp);
        return Color.Lerp(Color.blue, Color.red, (float)t);
    }

    //그리드 보여주기
    public void ShowGrids()
    {
        if (api_test.Instance == null || !api_test.Instance.IsGeoJsonLoaded)
        {
            Debug.LogWarning(" 데이터 아직 로딩 안됨");
            return;
        }
        VisualizeWeatherGrids();
    }


    private void ClearExistingGrids()
    {
        foreach (var obj in spawnedGrids) { if (obj != null) Destroy(obj); }
        spawnedGrids.Clear();
        foreach (var mat in generatedMaterials) { if (mat != null) Destroy(mat); }
        generatedMaterials.Clear();
    }

    void OnDestroy()
    {
        ClearExistingGrids();
        if (generatedBaseMaterial != null) Destroy(generatedBaseMaterial);
    }
}
