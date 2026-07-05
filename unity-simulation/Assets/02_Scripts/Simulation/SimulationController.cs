using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;



public class SimulationController : MonoBehaviour
{
    public static SimulationController Instance { get; private set; }

    // 건물의 상세 정보를 담을 구조체 정의
    [System.Serializable]
    public struct SpawnedAllBuildings
    {
        public string buildingID;
        public string zoneID;
        public float areaSize;
        public float height;
        public int floors;
        public double buildingCenterLon; // 경도 (정밀도를 위해 double 권장)
        public double buildingCenterLat; // 위도
    }

    // [새로운 구조체] 녹화 순위 및 점수 정보를 담을 구조체
    [System.Serializable]
    public struct GreeneryRankingBuildings
    {
        public int greeneryRank;       // zoneID 내에서의 순위
        public string buildingID;      // 건물 고유 ID
        public float areaSize;         // 건물 면적
        public float greeneryScore;    // 계산된 녹화 점수
        public GreeneryState greeneryStatus;  // 상태 문자열에서 Enum 형식으로 전환
    }

    // 계산 요청에 필요한 데이터를 한 번에 담아 Calculator로 보내기 위한 통신용 구조체
    public struct CalculationRequestData
    {
        public List<SpawnedAllBuildings> targetZoneBuildings;
        public List<SpawnedAllBuildings> allBuildings;
        public float greeneryRate;
    }

    // Calculator가 계산을 완료하고 Controller로 돌려줄 결과 데이터 구조체
    public struct CalculationResultData
    {
        public float totalAreaSize;
        public float expectedGreeneryArea;
        public float expectedTempEffect;
        public Dictionary<string, float> buildingScores; // Key: buildingID, Value: score
        public Dictionary<string, int> radiusCounts;     // Key: buildingID, Value: count
        public List<GreeneryRankingBuildings> rankedBuildings; // 랭킹/상태까지 계산된 최종 리스트

    }

    // Calculaotr에게 연산을 요청하는 이벤트
    public static event Action<CalculationRequestData> OnCalculationRequested;
    public static event Action OnResultDataUpdated;

    // 녹화 점수 계산에 필요한 변수들
    [Header("Simulation Settings")]
    public int radiusBuildingCount; // 반경내 건물 카운트
    public int greeneryBuildingcount; // 녹화건물 카운트
    public float totalAreaSize; // zoneID에 해당하는 건물중에 areaSize를 더해야함
    public float expectedGreeneryArea; // 전체 면적 * 목표 녹화율
    public double expectedTempEffect;
    public double greeneryAfterTemp;
    public float greeneryRate; //ControlPanel로부터 전달받은 녹화율 저장

    [Header("Current Simulation Target")]
    public string currentTargetZoneID = "0";
    public double gridOriginalTemp = 0.0; // [추가] 선택된 격자의 기존 온도를 저장할 변수

    // 모든 Building 스폰이후 데이터 수집 시작
    [Header("Simulation State")]
    public bool isBuildingDataLoaded = false;    // 데이터 수집(스폰) 완료 여부
    private int lastCheckCount = -1;             // 직전 프레임에 확인한 건물 수
    private Coroutine checkCompleteCoroutine;    // 스폰 완료 감지용 코루틴 저장소

    // 모든 Building 스폰 이후 데이터 수집 리스트
    [Header("Spawned Buildings Data")]
    public List<SpawnedAllBuildings> spawnedAllBuildings = new List<SpawnedAllBuildings>();

    [Header("Processed Greenery Data")]
    public List<GreeneryRankingBuildings> greeneryRankingBuildings = new List<GreeneryRankingBuildings>();

    // [중요 추가] 스폰된 빌딩들의 BuildingInfo 컴포넌트 실시간 캐싱 딕셔너리
    // 씬 탐색 병목 현상(FindObjectsOfType)을 방지하고, ID 기반 O(1) 초고속 대입이 가능하게 합니다.
    private Dictionary<string, BuildingInfo> _buildingInfoCache = new Dictionary<string, BuildingInfo>();


    private void Awake()
    {
        if (Instance != null && Instance != this) 
        {
            Debug.LogWarning($"[SimulationController] 중복된 오브젝트가 발견되어 {gameObject.name}을 파괴합니다.");
            Destroy(gameObject); return; 
        }
        Instance = this;
    }

    private void Start()
    {
        // [구독] BuildingManager가 건물을 생성할 때 발생하는 이벤트를 구독합니다.
        BuildingManager.OnBuildingZoneAssigned += AddSpawnedBuilding;
        // [구독] Calculator가 연산을 끝내고 던지는 이벤트를 구독합니다.
        GreeneryCalculator.OnCalculationCompleted += OnReceiveCalculationResult;
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 오브젝트 파괴 시 이벤트 구독을 해제합니다.
        BuildingManager.OnBuildingSpawned -= AddSpawnedBuilding;
        // [구독해지]
        GreeneryCalculator.OnCalculationCompleted -= OnReceiveCalculationResult;
    }

    /// BuildingManager에서 건물이 스폰될 때마다 자동으로 호출되어 리스트에 데이터를 적재합니다.
    private void AddSpawnedBuilding(BuildingData data, GameObject buildingObj)
    {
        if (data == null) return;

        // 중복 방지 (이미 리스트에 들어와 있는 건물인지 ID 검사)
        if (spawnedAllBuildings.Any(b => b.buildingID == data.id)) return;

        // SpawnedAllBuildings 구조체 생성 및 데이터 변환 매핑
        SpawnedAllBuildings newBuilding = new SpawnedAllBuildings();

        newBuilding.buildingID = data.id;

        // GeoJSON 내부 속성(Properties) 명칭에 따라 "zone_id", "zoneID", "zone" 등으로 매핑 조절 필요
        // BuildingData 내부에 구역 ID가 string 형태로 보관되어 있다고 가정합니다.

        // BuildingInfo 컴포넌트에서 zoneId를 가져와 string으로 변환하여 매핑
        // 보통 쓰고있는 zoneID가 string이기 때문에, BuildingInfo에는 int 라서 string으로 변환해줌
        BuildingInfo info = buildingObj.GetComponent<BuildingInfo>();
        if (info != null)
        {
            // zone 밖 건물은 zoneId가 null일 수 있으므로 빈 문자열로 안전하게 처리
            newBuilding.zoneID = info.zoneId ?? string.Empty;

            // [핵심] BuildingID에 해당하는 컴포넌트를 딕셔너리에 캐싱해 둡니다.
            if (!_buildingInfoCache.ContainsKey(data.id))
            {
                _buildingInfoCache.Add(data.id, info);
            }

        }
        else
        {
            newBuilding.zoneID = string.Empty;
            Debug.LogWarning($"[SimulationController] {data.id} 건물에서 BuildingInfo 컴포넌트를 찾을 수 없습니다.");
        }

        newBuilding.areaSize = (float)data.areaSize; // 원시 데이터가 double일 경우 캐스팅
        newBuilding.height = (float)data.height;
        newBuilding.floors = data.floors;
        newBuilding.buildingCenterLon = data.lon;
        newBuilding.buildingCenterLat = data.lat;

        // 리스트에 추가
        spawnedAllBuildings.Add(newBuilding);

        // 필요 시 실시간 디버그 로그 활성화
        //Debug.Log($"[SimulationController] 새 건물 수집! ID: {newBuilding.buildingID} " +
        //    $"| 현재 수집된 건물: {spawnedAllBuildings.Count}개");
    }

    /// <summary>
    /// BuildingManager가 건물을 모두 스폰한 후, 이 메서드(SetSpawnedBuildingsList)를 호출하여 리스트를 채우도록 합니다.
    /// </summary>
    public void SetSpawnedBuildingsList(List<SpawnedAllBuildings> buildingsData)
    {
        spawnedAllBuildings = buildingsData;

        // 데이터 강제 인입 시에는 씬 내 컴포넌트들을 재탐색하여 캐시를 다시 구성합니다.
        _buildingInfoCache.Clear();
        BuildingInfo[] allInfos = FindObjectsByType<BuildingInfo>(FindObjectsSortMode.None); foreach (var info in allInfos)
        {
            if (info.data != null && !string.IsNullOrEmpty(info.data.id))
            {
                _buildingInfoCache[info.data.id] = info;
            }
        }

        Debug.Log($"[SimulationController] 총 {spawnedAllBuildings.Count}개의 건물 데이터가 성공적으로 로드되었습니다.");
    }


    /// <summary>
    /// ControlPanel의 startGreeneryButton이 눌렸을 때 호출되는 진입점.
    /// zone 정보(zoneId, temperature)와 목표 녹화율을 한 번에 받아 시뮬레이션을 시작합니다.
    /// </summary>
    public void StartGreenerySimulationForZone(string zoneId, float zoneTemp, float greeneryRateValue)
    {
        if (string.IsNullOrEmpty(zoneId))
        {
            Debug.LogWarning("[SimulationController] 유효하지 않은 zoneID입니다.");
            return;
        }

        // 목표 녹화율 저장
        greeneryRate = greeneryRateValue;

        // 새 요청이므로 스폰 완료 감지 플래그를 초기화 (그리드 건물이 새로 스폰 중일 수 있음)
        isBuildingDataLoaded = false;
        lastCheckCount = -1;

        Debug.Log($"[SimulationController] 옥상 녹화 시뮬레이션 시작 — Zone: {zoneId}, 기존 온도: {zoneTemp}, 목표 녹화율: {greeneryRateValue}%");

        ProcessBuildingGreeneryRankings(zoneId, zoneTemp);
    }


    public void ProcessBuildingGreeneryRankings(string zoneID, double zoneTemp)
    {
        // 0. 전달받은 격자 ID와 기존 온도를 전역 변수에 저장
        currentTargetZoneID = zoneID;
        gridOriginalTemp = zoneTemp;

        Debug.Log($"[SimulationController] [{currentTargetZoneID}] 그리드 정보를 가져옵니다.");

        if (spawnedAllBuildings == null || spawnedAllBuildings.Count == 0)
        {
            Debug.LogWarning("[SimulationController] 원본 데이터가 없습니다.");
            return;
        }

        // 아직 스폰(수집) 완료 상태가 아니라면 계산을 유보하고, 감지 코루틴을 돌립니다.
        if (!isBuildingDataLoaded)
        {
            if (checkCompleteCoroutine != null)
                StopCoroutine(checkCompleteCoroutine);

            checkCompleteCoroutine = StartCoroutine(CheckSpawnCompletionLoop());
            return;
        }

        // 1. 구역 필터링
        var targetZoneBuildings = spawnedAllBuildings.Where(b => b.zoneID == currentTargetZoneID).ToList();

        // 2. [이벤트 발행] 직접 계산하지 않고 계산에 필요한 바구니를 채워 이벤트를 던집니다.
        CalculationRequestData request = new CalculationRequestData
        {
            targetZoneBuildings = targetZoneBuildings,
            allBuildings = spawnedAllBuildings,
            greeneryRate = this.greeneryRate
        };

        Debug.Log($"[SimulationController] [{currentTargetZoneID}] 구역 연산 요청을 전달합니다.");
        OnCalculationRequested?.Invoke(request);

    }

    // 실시간으로 스폰되는 건물 개수를 감시하여 완료를 판단하는 코루틴
    private IEnumerator CheckSpawnCompletionLoop()
    {
        Debug.Log("[SimulationController] 건물 스폰 상태 감지를 시작합니다...");

        while (!isBuildingDataLoaded)
        {
            // 현재까지 리스트에 쌓인 건물 수 기록
            lastCheckCount = spawnedAllBuildings.Count;

            // 0.5초 동안 대기하면서 BuildingManager가 스폰을 계속 하는지 지켜봅니다.
            yield return new WaitForSeconds(0.5f);

            // 0.5초가 지났는데도 전체 건물 개수(Count)에 변화가 없다면 스폰 루프가 끝난 것입니다.
            if (spawnedAllBuildings.Count > 0 && spawnedAllBuildings.Count == lastCheckCount)
            {
                isBuildingDataLoaded = true; // 완료 플래그 켜기
                Debug.Log($"[SimulationController] 더 이상 추가 스폰 없음 감지! 총 {spawnedAllBuildings.Count}개 수집 완료.");

                // 데이터 수집이 완전히 끝났으니, 멈췄던 연산을 다시 호출합니다.
                ProcessBuildingGreeneryRankings(currentTargetZoneID, gridOriginalTemp);
                yield break;
            }
        }
    }

    //[이벤트 콜백] GreeneryCalculator가 연산을 완료하면 자동으로 호출되는 구독 함수
    private void OnReceiveCalculationResult(CalculationResultData result)
    {
        // 3. Calculator가 돌려준 결과값들 전역 변수에 동기화
        totalAreaSize = result.totalAreaSize;
        expectedGreeneryArea = result.expectedGreeneryArea;
        expectedTempEffect = result.expectedTempEffect;
        greeneryAfterTemp = gridOriginalTemp - expectedTempEffect;

        Debug.Log($"[{currentTargetZoneID}] 구역 결과 수신 | 기존: {gridOriginalTemp}°C | 감소: {expectedTempEffect}°C | 예상: {greeneryAfterTemp}°C");
        
        // 4. Calculator가 랭킹/상태 계산까지 완료한 리스트를 그대로 사용
        greeneryRankingBuildings = result.rankedBuildings;

        // 1. 녹화 적용된(= Priority 이상) 건물 카운트 집계
        greeneryBuildingcount = greeneryRankingBuildings
            .Count(b => b.greeneryStatus == GreeneryState.GreeneryPriority || b.greeneryStatus == GreeneryState.GreeneryTop10);


        // 2. [가장 중요한 변경사항] 씬 내부의 BuildingInfo 실시간 컴포넌트 연동 업데이트
        // 이 루프를 통해 랭킹 리스트의 연산 값이 실제 씬 컴포넌트에 반영됩니다.
        foreach (var rData in greeneryRankingBuildings)
        {
            if (_buildingInfoCache.TryGetValue(rData.buildingID, out BuildingInfo info))
            {
                // 실시간 계산 결과 주입
                info.greeneryScore = rData.greeneryScore;
                info.greeneryRank = rData.greeneryRank;
                info.greeneryStatus = rData.greeneryStatus; // Enum 형 데이터 0, 1, 2 상태값 자동 주입

                // 반경 내 인접 건물 개수 동기화
                if (result.radiusCounts.TryGetValue(rData.buildingID, out int rCount))
                {
                    info.radiusBuildingCount = rCount;
                }
            }
        }


        Debug.Log($"[SimulationController] {currentTargetZoneID}번 구역 랭킹 정렬 완료 " +
            $"(총 {greeneryRankingBuildings.Count}개, 녹화 적용 {greeneryBuildingcount}개), 개별 BuildingInfo 컴포넌트 갱신 성공)");

        //데이터 세팅이 끝난 후, 결과 패널(UI)에 전달
        OnResultDataUpdated?.Invoke();

    }

}
