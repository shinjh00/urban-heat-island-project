using CesiumForUnity;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static UnityEditor.Progress;

// 구 영역을 그리드로 분할해서 지도 위에 zone 영역 생성
public class ZoneGenerator : MonoBehaviour
{
    [Header("Cesium References")]
    public CesiumGeoreference georeference;

    public List<GameObject> spawnedGrids = new List<GameObject>();

    // 생성된 그리드들을 담을 부모 빈 오브젝트 (한데 묶기 위함)
    [SerializeField]
    private Transform grids;

    private ZoneRenderer zoneRenderer;

    private bool gridsVisible = false;   // 격자 표시 여부, 기본은 숨김


    private void Start()
    {
        if (georeference == null)
            georeference = FindFirstObjectByType<CesiumGeoreference>();

        if (grids == null)
        {
            Debug.LogWarning("[ZoneGenerator] Grids 없음. Grids를 인스펙트 창에서 할당해주세요.");
            grids = georeference.transform.Find("Grids");
        }

        zoneRenderer = ZoneManager.Instance.zoneRenderer;
        zoneRenderer.InitRuntimeMaterial();
    }

    #region ``그리드 표시 함수``
    public void VisualizeGrids()
    {
        List<ZoneData> zoneGrids = ZoneManager.Instance.ZoneList;
        if (zoneGrids == null || zoneGrids.Count == 0) return;

        ClearExistingGrids();

        foreach (ZoneData grid in zoneGrids)
        {
            if (grid.polygon == null || grid.polygon.Length < 4) continue;

            double3[] geoPoints = new double3[4];
            for (int i = 0; i < 4; i++)
            {
                geoPoints[i] = new double3(grid.polygon[i, 0], grid.polygon[i, 1], 0.0);
            }

            // 생성 함수로 원본 grid 데이터 보따리를 함께 넘겨줌
            StartCoroutine(BuildGridQuadRaycast(geoPoints, grid.temperature, grid.zoneId, grid.polygon));
        }
        Debug.Log($"[ZoneGenerator] VisualizeGrids 실행. 세슘 지면에 그리드 {zoneGrids.Count}개 배치 완료");
    }

    private IEnumerator BuildGridQuadRaycast(double3[] geoPoints, float temperature, string zoneId, double[,] polygon)
    {
        if (georeference == null) yield break;

        double avgLon = (geoPoints[0].x + geoPoints[1].x + geoPoints[2].x + geoPoints[3].x) / 4.0;
        double avgLat = (geoPoints[0].y + geoPoints[1].y + geoPoints[2].y + geoPoints[3].y) / 4.0;

        double finalTerrainHeight = 40.0;

        double3 highEcef = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(new double3(avgLon, avgLat, 800.0));
        double3 highUnity = georeference.TransformEarthCenteredEarthFixedPositionToUnity(highEcef);
        Vector3 rayOrigin = new Vector3((float)highUnity.x, (float)highUnity.y, (float)highUnity.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 1500f))
        {
            double3 hitEcef = georeference.TransformUnityPositionToEarthCenteredEarthFixed(new double3(hit.point.x, hit.point.y, hit.point.z));
            double3 hitGeo = CesiumWgs84Ellipsoid.EarthCenteredEarthFixedToLongitudeLatitudeHeight(hitEcef);

            // 수치를 5.0m로 넉넉히 올려 산악/경사 지형 파묻힘 방지
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
        obj.name = $"Zone_{zoneId} [{temperature:F1}°C]";
        if (grids != null) obj.transform.SetParent(grids);

        CesiumGlobeAnchor anchor = obj.AddComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = centerGeo;

        obj.transform.localScale = new Vector3(width, height, 1f);
        obj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // 생성된 쿼드 객체에 컴포넌트를 붙이고 원본 데이터를 통째로 이식
        ZoneItem item = obj.AddComponent<ZoneItem>();
        item.data.zoneId = zoneId;
        item.data.temperature = temperature;
        item.data.polygon = polygon;

        // 개별 머티리얼 및 색상 적용
        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            zoneRenderer.ApplyTemperatureStyle(mr, temperature);
        }

        spawnedGrids.Add(obj);
        gridsVisible = true;
        yield return null;
    }

    public void ShowGrids()
    {
        ApplyVisibility();
        Debug.Log("[ZoneGenerator] ShowGrids 실행. 그리드 표시 ON");
    }

    private void ApplyVisibility()
    {
        foreach (GameObject grid in spawnedGrids)
        {
            if (grid == null) continue;
            MeshRenderer mr = grid.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = gridsVisible;
            Collider col = grid.GetComponent<Collider>();
            if (col != null) col.enabled = gridsVisible;
        }
    }
    #endregion


    #region ``그리드 선택 함수``
    public void HideOtherGrids(ZoneItem selectedItem)
    {
        foreach (GameObject grid in spawnedGrids)
        {
            // 선택된 구역 제외하고 전부 그리드 끄기
            bool isSelected = grid == selectedItem.gameObject;
            grid.GetComponent<MeshRenderer>().enabled = isSelected;
            grid.GetComponent<Collider>().enabled = isSelected;
        }
    }

    public IEnumerator BlinkAndLoad(ZoneItem selectedItem)
    {
        // 깜빡임 로직을 가져와서 사용
        MeshRenderer mr = selectedItem.GetComponent<MeshRenderer>();
        yield return StartCoroutine(zoneRenderer.BlinkEffect(mr));

        // 중심점 찾기
        (double lon, double lat) = selectedItem.GetCenter();

        // 반경 범위 건물 스폰
        BuildingManager.Instance.FocusOnGrid(lon, lat);
    }
    #endregion
    

    private void ClearExistingGrids()
    {
        if (grids != null)
        {
            foreach (Transform child in grids)
            {
                Destroy(child.gameObject);
            }
        }
        spawnedGrids.Clear();
    }

    void OnDestroy()
    {
        ClearExistingGrids();
    }

}
