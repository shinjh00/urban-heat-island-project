//using CesiumForUnity;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using Unity.Mathematics;
//using UnityEngine;
//using UnityEngine.InputSystem; // New Input System 패키지 대응

//public class WeatherGridVisualizer : MonoBehaviour
//{

//    [Header("Dynamic Range (Auto Setup)")]
//    private double dynamicMinTemp = -30.0f;
//    private double dynamicMaxTemp = 50.0f;




//    void Start()
//    {
//        if (geoRef == null) geoRef = FindFirstObjectByType<CesiumGeoreference>();
//        if (georeferenceTransform == null && geoRef != null) georeferenceTransform = geoRef.transform;

//        InitRuntimeMaterial();
//    }

//    void Update()
//    {
//        // 마우스 왼쪽 버튼 클릭 감지
//        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
//        {
//            Vector2 mousePosition = Mouse.current.position.ReadValue();
//            Ray ray = Camera.main.ScreenPointToRay(mousePosition);

//            if (Physics.Raycast(ray, out RaycastHit hit))
//            {
//                WeatherGridItem gridItem = hit.collider.GetComponent<WeatherGridItem>();
//                if (gridItem != null)
//                {
//                    Debug.Log($" [격자 선택] ID: {gridItem.gridId}번만 표시합니다.");
//                    Debug.Log($" [격자 선택] 온도: {gridItem.temperature}");
//                    for (int i = 0; i < gridItem.polygonPoints.Count; i++)
//                    {
//                        double lon = gridItem.polygonPoints[i][0];
//                        double lat = gridItem.polygonPoints[i][1];
//                        Debug.Log($" 꼭짓점 [{i + 1}] -> [ 경도: {lon:F8}, 위도: {lat:F8} ]");
//                    }

//                    //  모든 격자를 일단 다 숨깁니다.
//                    foreach (var grid in spawnedGrids)
//                    {
//                        if (grid != null) grid.GetComponent<MeshRenderer>().enabled = false;
//                    }

//                    //  클릭한 이 격자 하나만 켭니다.
//                    MeshRenderer selectedMr = hit.collider.GetComponent<MeshRenderer>();
//                    selectedMr.enabled = true;

//                    //  추가: Blink → 중심계산 → 건물 로드
//                    if (blinkRoutine != null) StopCoroutine(blinkRoutine);
//                    blinkRoutine = StartCoroutine(BlinkAndLoad(gridItem, selectedMr));
//                }
//                else
//                {
//                    // 격자가 아닌 일반 지면이나 허공을 클릭하면 모든 격자를 다시 숨깁니다.
//                    foreach (var grid in spawnedGrids)
//                    {
//                        if (grid != null) grid.GetComponent<MeshRenderer>().enabled = false;
//                    }
//                }
//            }
//        }
//    }





//    public void VisualizeWeatherGrids()
//    {
//        if (api_test.Instance == null || !api_test.Instance.IsGeoJsonLoaded)
//        {
//            Debug.LogWarning("[시각화 실패] GeoJSON 데이터가 아직 로드되지 않았습니다.");
//            return;
//        }

//        var weatherGrids = api_test.Instance.CachedWeatherGrids;
//        if (weatherGrids == null || weatherGrids.Count == 0) return;

//        ClearExistingGrids();

//        float currentMin = float.MaxValue;
//        float floatMax = float.MinValue;
//        foreach (var grid in weatherGrids)
//        {
//            float val = (float)grid.temperature;
//            if (val < currentMin) currentMin = val;
//            if (val > floatMax) floatMax = val;
//        }
//        bool isApprox = Mathf.Approximately(currentMin, floatMax);
//        dynamicMinTemp = isApprox ? currentMin - 0.5f : currentMin;
//        dynamicMaxTemp = isApprox ? floatMax + 0.5f : floatMax;

//        foreach (var grid in weatherGrids)
//        {
//            if (grid.polygonPoints == null || grid.polygonPoints.Count < 4) continue;

//            double3[] geoPoints = new double3[4];
//            for (int i = 0; i < 4; i++)
//            {
//                geoPoints[i] = new double3(grid.polygonPoints[i][0], grid.polygonPoints[i][1], 0.0);
//            }

//            // 생성 함수로 원본 grid 데이터 보따리를 함께 넘겨줍니다.
//            StartCoroutine(BuildWeatherQuadRaycast(geoPoints, (float)grid.temperature, grid.id, grid.polygonPoints));
//        }

//        Debug.Log($" [3/3 완료] 세슘 지면 위에 클릭 가능한 격자 {weatherGrids.Count}개 배치 완료.");
//    }


//    private Color GetColorFromTemperature(double temp)
//    {
//        double t = Mathf.InverseLerp((float)dynamicMinTemp, (float)dynamicMaxTemp, (float)temp);
//        return Color.Lerp(Color.blue, Color.red, (float)t);
//    }

//    private void ClearExistingGrids()
//    {
//        foreach (var obj in spawnedGrids) { if (obj != null) Destroy(obj); }
//        spawnedGrids.Clear();
//        foreach (var mat in generatedMaterials) { if (mat != null) Destroy(mat); }
//        generatedMaterials.Clear();
//    }

//    void OnDestroy()
//    {
//        ClearExistingGrids();
//        if (generatedBaseMaterial != null) Destroy(generatedBaseMaterial);
//    }
//}
