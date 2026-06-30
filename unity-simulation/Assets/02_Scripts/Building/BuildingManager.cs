using CesiumForUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

// 건물 생명주기를 관리하는 싱글톤 클래스
public class BuildingManager : MonoBehaviour
{
    // 싱글톤
    public static BuildingManager Instance { get; private set; }

    [Header("Cesium")]
    public CesiumGeoreference georeference;
    public Cesium3DTileset terrainTileset;

    [Header("Spawn Settings")]
    // 카메라 반경 몇m 안의 건물만 스폰할지
    public float activationRadius = 1000f;
    // 씬에 최대 몇 개의 건물을 보이게 할것인지
    public int maxBuildings = 10000;
    // 몇초마다 카메라 위치를 확인하여 스폰/제거를 반복할지
    public float checkInterval = 5f;
    
    [Header("GeoJSON")]
    // StreamingAssets 폴더에 있는 GeoJSON 파일 이름
    public string geoJsonFileName = "mapo_building_final.geojson";


    public BuildingInfo selectedBuilding { get; private set; }  // 현재 선택된 건물

    // 건물 스폰 완료 시 발생 이벤트
    public static event Action<BuildingData, GameObject> OnBuildingSpawned;

    // 건물 제거 시 발생 이벤트
    public static event Action<string> OnBuildingRemoved;

    // 건물 선택 시 발생 이벤트
    public static event Action<BuildingInfo> OnBuildingSelected;

    // 건물 선택 해제 시 발생 이벤트
    public static event Action OnBuildingDeselected;

    // 기본 건물 머테리얼
    public Material defaultMaterial;

    // 현재 씬에 활성화된 건물 딕셔너리 (건물 id : GameObject)
    private Dictionary<string, GameObject> activeBuildings = new Dictionary<string, GameObject>();

    // DataParser 가 반환한 전체 건물 데이터 리스트
    private List<BuildingData> buildingDataList = new List<BuildingData>();

    // 카메라 현재 위경도
    private double camLon, camLat;

    // 스폰 성공/실패 통계 (디버그용)
    private int statSpawned;
    private int statFailed;


    //Jin 추가부분
    // ───── 그리드 중심점에서 750m 건물 로드 ─────
    private bool gridMode = false;
    private double zoneCenterLon, zoneCenterLat;
    public float gridLoadRadius = 500f;


    // 시뮬레이션 시작 전엔 어떤 스폰도 하지 않도록 막는 마스터 스위치
    private bool spawnEnabled = false;



    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(InitializeAsync());
    }


    // GeoJSON 비동기 파싱 후 스폰 루프 시작
    IEnumerator InitializeAsync()
    {
        DataParser parser = new DataParser();
        System.Threading.Tasks.Task<List<BuildingData>> parseTask
            = parser.ParseGeoJson(geoJsonFileName);

        // 파싱 완료 대기 (그동안 Unity 씬은 계속 돌아감)
        yield return new WaitUntil(() => parseTask.IsCompleted);

        // 파싱 결과 받기
        buildingDataList = parseTask.Result;
        Debug.Log("[BuildingManager] 파싱 완료. 총 " + buildingDataList.Count + "개");

        // 스폰 루프 시작
        StartCoroutine(UpdateBuildingsLoop());
    }


    // 주기적으로 스폰/제거 반복
    IEnumerator UpdateBuildingsLoop()
    {
        // Cesium 지형이 로딩될 시간을 줌
        yield return new WaitForSeconds(3f);

        while (true)
        {
            // 그리드 모드면 카메라 추적 멈추기
            // 구역 선택 전(spawnEnabled == false)이면 아무 것도 안 하고 대기
            if (!spawnEnabled)
            {
                yield return new WaitForSeconds(checkInterval);
                continue;
            }

            // 그리드 모드면 카메라 추적 멈추기
            if (gridMode)
            {
                yield return new WaitForSeconds(checkInterval);
                continue;
            }
            // 1) 카메라 위경도 갱신
            UpdateCameraPosition();
            // 2) 반경 안 건물 스폰
            yield return StartCoroutine(SpawnNearbyBuildings());
            // 3) 반경 밖 건물 제거
            RemoveFarBuildings();
            // 4) checkInterval 초 대기 후 반복
            yield return new WaitForSeconds(checkInterval);
        }
    }


    // Dynamic 카메라 위경도 갱신 : Unity 좌표 -> ECEF(지구중심좌표) -> 위경도 좌표
    void UpdateCameraPosition()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 camPos = cam.transform.position;
        double3 camECEF = georeference.TransformUnityPositionToEarthCenteredEarthFixed(
                              new double3(camPos.x, camPos.y, camPos.z));
        double3 camLLH = CesiumWgs84Ellipsoid.EarthCenteredEarthFixedToLongitudeLatitudeHeight(camECEF);

        camLon = camLLH.x;
        camLat = camLLH.y;
    }


    // 반경 안 건물 스폰
    IEnumerator SpawnNearbyBuildings()
    {
        // LINQ로 조건에 맞는 건물만 필터링
        List<BuildingData> nearby = buildingDataList
            .Where(d => IsWithinRadius(d))
            .Where(d => !IsAlreadySpawned(d))
            .OrderBy(d => GetDistanceMeters(camLat, camLon, d.lat, d.lon))
            .Take(maxBuildings - activeBuildings.Count)
            .ToList();

        int count = 0;
        foreach (BuildingData data in nearby)
        {
            SpawnBuilding(data);
            count++;
        }

        if (count > 0)
            Debug.Log("[BuildingManager] 스폰 시도: " + count + " | 성공:" + statSpawned
                      + " 실패:" + statFailed + " | 활성:" + activeBuildings.Count);
        yield break;
    }

    // 건물 오브젝트 하나를 생성할 때 실행되는 내용
    // BuildingData로 건물 GameObject를 생성하고 씬에 배치
    private void SpawnBuilding(BuildingData data)
    {
        string key = data.id ?? Guid.NewGuid().ToString();

        if (activeBuildings.ContainsKey(key)) return;
        if (GameObject.Find("Building_" + key) != null) return;

        // 1) MeshBuilder로 메시 생성
        Mesh mesh = MeshBuilder.BuildPolygonMesh(data.polygon, data.height);
        if (mesh == null)
        {
            statFailed++;
            return;
        }

        // 2) GameObject 생성 및
        // MeshFilter, MeshRenderer, MeshCollider, BuildingInfo 컴포넌트 부착
        GameObject obj = new GameObject("Building_" + key);
        obj.transform.SetParent(georeference.transform);
        activeBuildings[key] = obj;
        statSpawned++;

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.enabled = false;
        mr.material = defaultMaterial;
        MeshCollider mc = obj.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        BuildingInfo info = obj.AddComponent<BuildingInfo>();
        info.data = data;

        // 3) PlaceBuilding() 코루틴으로 Cesium 지형 위에 배치
        StartCoroutine(PlaceBuilding(obj, data.lon, data.lat));

        // 4) 건물 생성이 끝났음을 알리는 이벤트 트리거
        OnBuildingSpawned?.Invoke(data, obj);
    }


    // 카메라 반경 밖으로 벗어난 건물 제거
    void RemoveFarBuildings()
    {
        List<string> toRemove = new List<string>();

        foreach (KeyValuePair<string, GameObject> kvp in activeBuildings)
        {
            BuildingData data = buildingDataList.Find(d => d.id == kvp.Key);
            if (data == null) {
                toRemove.Add(kvp.Key); continue;
            }
            if (GetDistanceMeters(camLat, camLon, data.lat, data.lon) > activationRadius * 1.5f)
            {
                Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (string key in toRemove)
        {
            activeBuildings.Remove(key);
            OnBuildingRemoved?.Invoke(key);
        }

        if (toRemove.Count > 0)
            Debug.Log("[BuildingManager] 제거: " + toRemove.Count + " | 활성:" + activeBuildings.Count);
    }


    // Cesium 지형 높이를 샘플링하여 올바른 위치에 건물 배치
    IEnumerator PlaceBuilding(GameObject obj, double lon, double lat)
    {
        yield return null;
        if (obj == null) yield break;

        CesiumGlobeAnchor anchor = obj.AddComponent<CesiumGlobeAnchor>();
        yield return new WaitUntil(() => anchor != null && anchor.isActiveAndEnabled);

        double terrainHeight = 0;
        if (terrainTileset != null)
        {
            double3[] positions = new double3[] { new double3(lon, lat, 0) };
            System.Threading.Tasks.Task<CesiumSampleHeightResult> task
                = terrainTileset.SampleHeightMostDetailed(positions);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Result.sampleSuccess[0])
                terrainHeight = task.Result.longitudeLatitudeHeightPositions[0].z;
        }
        anchor.longitudeLatitudeHeight = new double3(lon, lat, terrainHeight);

        yield return null;

        if (obj != null)
            obj.GetComponent<MeshRenderer>().enabled = true;
    }


    // 추가 부분 그리드 관련 함수
    public void FocusOnGrid(double centerLon, double centerLat)
    {
        gridMode = true;
        spawnEnabled = true;
        zoneCenterLon = centerLon;
        zoneCenterLat = centerLat;

        Debug.Log($"[BuildingManager] 그리드 로드 시작 — 중심({centerLon:F6}, {centerLat:F6})");
        StartCoroutine(LoadGridBuildings());
    }

    private IEnumerator LoadGridBuildings()
    {
        int count = 0;
        foreach (BuildingData data in buildingDataList)
        {
            double dist = GetDistanceMeters(zoneCenterLat, zoneCenterLon, data.lat, data.lon);
            if (dist > gridLoadRadius) continue;

            if (IsAlreadySpawned(data)) continue;

            SpawnBuilding(data);
            count++;

            if (count % 30 == 0) yield return null;
        }

        Debug.Log($"[BuildingManager] 그리드 건물 로드 완료 — {count}개");
    }





    #region ``주요 함수에 필요한 보조 함수들``
    // 건물이 카메라 활성 반경 안에 있는지 확인
    private bool IsWithinRadius(BuildingData data)
        => GetDistanceMeters(camLat, camLon, data.lat, data.lon) <= activationRadius;

    // 건물이 이미 스폰되어 있는지 확인
    private bool IsAlreadySpawned(BuildingData data)
        => activeBuildings.ContainsKey(data.id ?? "");

    // 두 위경도 좌표 사이의 거리를 m 단위로 계산
    private double GetDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        double dlat = (lat2 - lat1) * 111320.0;
        double dlon = (lon2 - lon1) * 111320.0 * Math.Cos(lat1 * Math.PI / 180.0);
        return Math.Sqrt(dlat * dlat + dlon * dlon);
    }
    #endregion


    // 활성 건물 전체 딕셔너리 반환
    public IReadOnlyDictionary<string, GameObject> GetActiveBuildings()
        => activeBuildings;

    // id로 스폰된 건물 GameObject를 반환
    public GameObject GetBuilding(string id)
    {
        activeBuildings.TryGetValue(id, out GameObject obj);
        return obj;
    }

    #region ``BuildingSelector.cs 관련 함수``
    // 건물 선택 이벤트 실행
    public void SelectBuilding(BuildingInfo info)
    {
        if (selectedBuilding != null)
        {
            OnBuildingDeselected?.Invoke();  // 기존 건물 있으면 해제
        }
        selectedBuilding = info;
        OnBuildingSelected?.Invoke(info);
    }

    // 건물 선택 해제 이벤트 실행
    public void DeselectBuilding()
    {
        if (selectedBuilding == null)
            return;

        selectedBuilding = null;
        OnBuildingDeselected?.Invoke();
    }
    #endregion


}