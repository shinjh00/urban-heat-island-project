using CesiumForUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 건물 생명주기를 관리하는 싱글톤 클래스.
///
/// 이 클래스가 하는 일:
/// 1. DataParser 로 GeoJSON 을 비동기로 파싱하여 건물 데이터를 가져온다.
/// 2. 카메라 위치를 주기적으로 확인한다.
/// 3. 카메라 반경 안의 건물만 MeshBuilder 로 만들어서 씬에 올린다.
/// 4. 카메라가 멀어진 건물은 씬에서 제거한다.
///
/// 주요 함수 목록:
/// - InitializeAsync()      : GeoJSON 비동기 파싱 후 스폰 루프 시작
/// - UpdateBuildingsLoop()  : 주기적으로 스폰/제거 반복
/// - UpdateCameraPosition() : 카메라 위경도 갱신
/// - SpawnNearbyBuildings() : 반경 안 건물 스폰
/// - SpawnBuilding()        : 건물 하나 생성
/// - RemoveFarBuildings()   : 멀어진 건물 제거
/// - PlaceBuilding()        : Cesium 지형 위에 건물 배치
/// </summary>
public class BuildingManager : MonoBehaviour
{
    // 싱글톤
    public static BuildingManager Instance { get; private set; }

    [Header("Cesium")]
    public CesiumGeoreference georeference;
    public Cesium3DTileset terrainTileset;

    [Header("Spawn Settings")]
    // 카메라 반경 500m 안의 건물만 스폰한다.
    public float activationRadius = 1000f;
    // 씬에 최대 500개까지만 건물을 올린다.
    public int maxBuildings = 10000;
    // 5초마다 카메라 위치를 확인하여 스폰/제거를 반복한다.
    public float checkInterval = 5f;
    
    [Header("GeoJSON")]
    // StreamingAssets 폴더에 있는 GeoJSON 파일 이름
    public string geoJsonFileName = "mapo_building_final.geojson";


    public BuildingInfo selectedBuilding { get; private set; }  // 현재 선택된 건물

    // 건물 스폰 완료 시 발생하는 이벤트.
    // BuildingVisualManager 에서 구독하여 머티리얼을 적용한다.
    public static event Action<BuildingData, GameObject> OnBuildingSpawned;

    // 건물 제거 시 발생하는 이벤트
    public static event Action<string> OnBuildingRemoved;

    // 건물 선택 시 발생하는 이벤트
    public static event Action<BuildingInfo> OnBuildingSelected;

    // 건물 선택 해제 시 발생하는 이벤트
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


    /// <summary>
    /// GeoJSON 을 비동기로 파싱하고 완료되면 스폰 루프를 시작한다.
    /// 전체 흐름:
    /// 1. ParseGeoJson() 호출 -> 백그라운드에서 파싱 시작, Task(영수증) 반환
    /// 2. WaitUntil() 로 매 프레임 Task 완료 여부 확인
    /// 3. 완료되면 Task.Result 로 BuildingData 리스트 받기
    /// 4. 건물 스폰 루프 시작
    /// 파싱하는 동안 Unity 씬은 계속 정상적으로 돌아간다.
    /// </summary>
    IEnumerator InitializeAsync()
    {
        DataParser parser = new DataParser();
        System.Threading.Tasks.Task<List<BuildingData>> parseTask
            = parser.ParseGeoJson(geoJsonFileName);

        yield return new WaitUntil(() => parseTask.IsCompleted);

        buildingDataList = parseTask.Result;
        Debug.Log("[BuildingManager] 파싱 완료. 총 " + buildingDataList.Count + "개");

        StartCoroutine(UpdateBuildingsLoop());
    }


    /// <summary>
    /// 주기적으로 카메라 위치를 확인하여 건물을 스폰하고 제거하는 루프.
    /// 3초 대기 후 시작하는 이유: Cesium 지형이 로딩될 시간을 준다.
    /// </summary>

    IEnumerator UpdateBuildingsLoop()
    {
        // Cesium 지형이 로딩될 시간을 준다.
        yield return new WaitForSeconds(3f);

        while (true)
        {
            UpdateCameraPosition();

            yield return StartCoroutine(SpawnNearbyBuildings());

            RemoveFarBuildings();

            yield return new WaitForSeconds(checkInterval);
        }
    }


    /// <summary>
    /// 카메라의 현재 위경도를 갱신한다.
    /// Unity 좌표 -> ECEF(지구 중심 좌표) -> 위경도 순서로 변환한다.
    /// </summary>
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


    /// <summary>
    /// 카메라 반경 안에 있는 건물을 스폰한다.
    /// LINQ 로 조건에 맞는 건물만 필터링하여 가까운 순서대로 스폰한다.
    /// 30개마다 yield return null 로 프레임을 나눠서 한 프레임에 너무 많이 생성되지 않게 한다.
    /// </summary>
    IEnumerator SpawnNearbyBuildings()
    {
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
            Debug.Log("[BuildingManager] 스폰 시도: " + count
                      + " | 성공:" + statSpawned
                      + " 실패:" + statFailed
                      + " | 활성:" + activeBuildings.Count);
        yield break;
    }

    /// <summary>
    /// BuildingData 로 건물 GameObject 를 생성하고 씬에 배치한다.
    /// 전체 흐름:
    /// 1. MeshBuilder 로 메시 생성
    /// 2. GameObject 에 MeshFilter, MeshRenderer, MeshCollider, BuildingInfo 부착
    /// 3. PlaceBuilding() 코루틴으로 Cesium 지형 위에 배치
    /// </summary>
    /// <summary>
    /// BuildingData 로 건물 GameObject 를 생성하고 씬에 배치한다.
    /// </summary>
    /// <summary>
    /// [동작 설명 3] BuildingData 정보를 해석하여 3D 메시 오브젝트를 생성하고 머티리얼을 적용합니다.
    /// </summary>
    private void SpawnBuilding(BuildingData data)
    {
        string key = data.id ?? Guid.NewGuid().ToString();

        if (activeBuildings.ContainsKey(key)) return;
        if (GameObject.Find("Building_" + key) != null) return;

        // ── 1단계: 3D 메시(외형 뼈대) 생성 ──
        Mesh mesh = MeshBuilder.BuildPolygonMesh(data.polygon, data.height);
        if (mesh == null)
        {
            statFailed++;
            return;
        }

        // ── 2단계: 유니티 GameObject 생성 및 컴포넌트 조립 ──
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

        // ── 3단계: Cesium 3D 지형 위에 밀착 배치 ──
        StartCoroutine(PlaceBuilding(obj, data.lon, data.lat));

        OnBuildingSpawned?.Invoke(data, obj);
    }


    /// <summary>
    /// 카메라 반경 밖으로 벗어난 건물을 제거한다.
    /// 반경의 1.5배 거리를 기준으로 삼는 이유:
    /// 정확히 반경 경계에서 스폰/제거가 반복되는 현상(깜빡임)을 방지하기 위해서다.
    /// </summary>
    /// 
    void RemoveFarBuildings()
    {
        List<string> toRemove = new List<string>();

        foreach (KeyValuePair<string, GameObject> kvp in activeBuildings)
        {
            BuildingData data = buildingDataList.Find(d => d.id == kvp.Key);
            if (data == null) { toRemove.Add(kvp.Key); continue; }

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


    /// <summary>
    /// Cesium 지형 높이를 샘플링하여 건물을 올바른 위치에 배치한다.
    /// CesiumGlobeAnchor : 위경도 좌표로 GameObject 위치를 고정하는 컴포넌트
    /// SampleHeightMostDetailed : 해당 위경도의 지형 높이를 가져오는 함수
    /// </summary>
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


    private bool IsWithinRadius(BuildingData data)
        => GetDistanceMeters(camLat, camLon, data.lat, data.lon) <= activationRadius;

    private bool IsAlreadySpawned(BuildingData data)
        => activeBuildings.ContainsKey(data.id ?? "");

    private double GetDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        double dlat = (lat2 - lat1) * 111320.0;
        double dlon = (lon2 - lon1) * 111320.0 * Math.Cos(lat1 * Math.PI / 180.0);
        return Math.Sqrt(dlat * dlat + dlon * dlon);
    }

    public IReadOnlyDictionary<string, GameObject> GetActiveBuildings()
        => activeBuildings;

    // id로 스폰된 건물 GameObject를 반환
    public GameObject GetBuilding(string id)
    {
        activeBuildings.TryGetValue(id, out GameObject obj);
        return obj;
    }

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

}