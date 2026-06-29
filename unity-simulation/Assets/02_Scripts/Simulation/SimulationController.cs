using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;



public class SimulationController : MonoBehaviour
{
    [Header("References")]
    public GreeneryCalculator calculator;

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
        public string greeneryStatus;  // 녹화 상태 (예: "greeneryTop10", "greeneryPriority", "normal" 등)
    }

    // 녹화 점수 계산에 필요한 변수들
    [Header("Simulation Settings")]
    public int radiusBuildingCount; // 반경내 건물 카운트
    public int greeneryBuildingcount; // 녹화건물 카운트
    public float totalAreaSize; // zoneID에 해당하는 건물중에 areaSize를 더해야함
    public float expectedGreeneryArea; // 전체 면적 * 목표 녹화율
    public double expectedTempEffect;
    public double greeneryAfterTemp;
    public float greeneryRate; //ControlPanel로부터 전달받은 녹화율 저장



    // BuildingManager에서 스폰된 건물들의 시뮬레이션 데이터 리스트
    [Header("Spawned Buildings Data")]
    public List<SpawnedAllBuildings> spawnedAllBuildings = new List<SpawnedAllBuildings>();

    [Header("Processed Greenery Data")]
    // 조건에 따라 정렬되어 최종 저장될 리스트
    public List<GreeneryRankingBuildings> greeneryRankingBuildings = new List<GreeneryRankingBuildings>();


    /// <summary>
    /// BuildingManager가 건물을 모두 스폰한 후, 이 메서드(SetSpawnedBuildingsList)를 호출하여 리스트를 채우도록 합니다.
    /// </summary>
    public void SetSpawnedBuildingsList(List<SpawnedAllBuildings> buildingsData)
    {
        spawnedAllBuildings = buildingsData;
        Debug.Log($"[SimulationController] 총 {spawnedAllBuildings.Count}개의 건물 데이터가 성공적으로 로드되었습니다.");
    }



    public void ProcessBuildingGreeneryRankings()
    {
        if (spawnedAllBuildings == null || spawnedAllBuildings.Count == 0)
        {
            Debug.LogWarning("[SimulationController] 원본 데이터가 없습니다.");
            return;
        }

        if (calculator == null)
        {
            Debug.LogError("[SimulationController] Calculator 스크립트 래퍼런스가 할당되지 않았습니다!");
            return;
        }

        // 1. targetZoneID에 해당하는 건물들만 필터링 (if문 역할을 하는 LINQ Where 사용)

        // 현재 활성화된 구역 ID를 targetZoneID 자리에 넣어주시면 됩니다.
        string targetZoneID = "1"; // 예시: 1번 구역만 거르고 싶을 때
        // zoneID 인식해서 targetZoneID 여기에 넣으면 됨
        
        var targetZoneBuildings = spawnedAllBuildings.Where(b => b.zoneID == targetZoneID).ToList();

        // GreeneryCalculator를 호출하여 전체옥상면적, 예상녹화면적 계산
        totalAreaSize = calculator.CalcTotalAreaSize(targetZoneBuildings);
        expectedGreeneryArea = calculator.CalcTotalGreeneryArea(totalAreaSize, greeneryRate);
        expectedTempEffect = calculator.CalcExpectedTempEffect(greeneryRate);


        Debug.Log($"[{targetZoneID}] 구역의 총 면적(totalAreaSize): {totalAreaSize} ㎡");

        // 2. 각 건물의 정보를 기반으로 Calculator에서 점수 가져오기
        List<GreeneryRankingBuildings> zoneBuildings = new List<GreeneryRankingBuildings>();

        foreach (var bSimData in targetZoneBuildings)
        {
            GreeneryRankingBuildings gData = new GreeneryRankingBuildings();
            gData.buildingID = bSimData.buildingID;
            gData.areaSize = bSimData.areaSize;

            // 1. GreeneryCalculator를 통해 해당 건물의 주변 반경 내 건물 수를 계산합니다.
            int currentRadiusCount = calculator.CalcRadiusBuildingCount(bSimData, spawnedAllBuildings);

            // [핵심] Calculator 스크립트의 CalcGreeneryScore 함수 호출 (areaSize, height 사용)
            gData.greeneryScore = calculator.CalcGreeneryScore(currentRadiusCount, bSimData.areaSize, bSimData.height);

            gData.greeneryStatus = "Normal";
            gData.greeneryRank = 0;

            zoneBuildings.Add(gData);
        }

        // 3. 해당 구역 내에서 점수가 높은 순으로 정렬 후 순위(Rank) 매기기
        List<GreeneryRankingBuildings> rankedZoneBuildings = zoneBuildings.OrderByDescending(b => b.greeneryScore).ToList();

        for (int i = 0; i < rankedZoneBuildings.Count; i++)
        {
            // 임시 변수에 구조체 값을 통째로 꺼내옵니다.
            GreeneryRankingBuildings updatedData = rankedZoneBuildings[i];

            // 꺼내온 변수의 데이터를 변경합니다.
            updatedData.greeneryRank = i + 1;
            updatedData.greeneryStatus = (i == 0) ? "GreeneryPriority" : "Normal";

            // greeneryStatus 부분 함수 추가 필요

            // 데이터가 바뀐 구조체를 리스트의 해당 자리에 다시 덮어씁니다!
            rankedZoneBuildings[i] = updatedData;
        }


        // 4. 최종 리스트 업데이트 (이미 정렬되어 있으므로 그대로 할당하거나 랭킹순으로 동기화)
        greeneryRankingBuildings = rankedZoneBuildings;

        Debug.Log($"[SimulationController] {targetZoneID}번 구역 내 총 {greeneryRankingBuildings.Count}개 건물의 순위 정렬이 완료되었습니다.");
    }




// 이하는 기존 코드(건물 선택시 그리드의 모든 건물 정보 가져오는 함수는 굳이 필요 없을 것 같음)


/*
         1. 시뮬레이션 시작 과정 전체
    1. 시뮬레이션 준비 버튼 누름
    2. 사용할 그리드 내에 있는 건물 선택
        1. debug.Log(그리드에 해당하는 건물을 선택해주세요) (왼쪽 아래 창)
        2. 이 건물 선택시 해당 그리드 안에 있는 모든 건물 잠깐 blink
        3. 해당 지역을 선택했습니다.
    3. 목표 녹화율 받음 (슬라이더)
        1. 목표 녹화율 n% 지정되었습니다
    4. 시뮬레이션 시작 버튼 누름
2. 그리드 내에 건물 정보 받아오기
    1. 받아오는 정보
        1. 그리드 관련 정보
            1. 온도정보
        2. 건물 관련 정보
            1. 옥상면적
            2. 건물 높이
3. 계산할 정보
    1. 목표 녹화율에 따른 목표 녹화 면적 계산
        1. 그리드 내에 있는 모든 건물의 옥상 면적 합
        2. 이 합에다가 목표 녹화율 곱해서 목표 녹화 면적 계산
    2. 반경 내 건물 수 계산
        1. 건물 별 중심점으로부터 250m 반경 내 건물 수
    3. 격자 내부에 있는 건물들 점수 계산
        1. 우선순위 함수 적용
        2. 각 건물별 우선순위 점수 할당
    4. 높은 점수 순서대로 리스트 제작
    5. 리스트 토대로 차례대로 목표 녹화 면적 까지 더함
    6. 목표 녹화 면적 충족시 계산 과정 종료
4. Top10만 우선녹화건물 색상 적용(진한 초록)
    1. Top10은 플로팅 패널로 표기
        1. 보는 시점 따라서 따라오는걸로
        2. 내용
            1. 우선순위 top10 순위 나옴
            2. 우선녹화점수 표기
    2. 우선녹화건물은 빌딩 정보에 상태 값 할당(enum 우선녹화건물)
    3. 해당 건물 클릭시 정보패널에 표기
5. 시뮬레이션 결과 출력(오른쪽 하단)
    1. 내용
        1. 목표 녹화율
        2. 녹화 건물 수
        3. 녹화 적용 면적
            1. 목표 녹화 면적 계산 값 (3-a-ii)
        4. 예상 감소 온도
            1. 녹화율에 비례한 온도감소 정도 표시
            2. 온도감소 정도는 4~6C (가정)
            3. 녹화율 (20%~80%)
            4. ex) 녹화율 80% 시 → 온도감소 6도 / 녹화율 50% 시 → 온도감소 5도
        5. 녹화 전 기존온도
            1. 그리드 온도
        6. 녹화 후 예상감소온도
            1. 그리드 온도 - 예상 효과 온도
6. 시뮬레이션 종료 메세지(왼쪽 하단)
    1. debug.log(시뮬레이션이 종료되었습니다)
 */



// 건물 정보 가져오기
public void BuildingInfoLoad()
{
    // 면적, 높이 가져오기
}

// 그리드 정보 가져오기
public void GridInfoLoad()
{
    // 그리드 좌표, 그리드 온도 가져오기
}

// 녹화 우선순위 순위, 점수를 건물 데이터에 할당
public void PutGreeneryScore()
{
    // 계산식을 통해서 얻은 순위, 점수를
    // 건물리스트를 돌면서 건물에 할당
}


// 건물 선택했을 때 그리드 내에 있는 모든 건물을 건물 리스트에 담기
// BuildingSelector에 있는 함수 호출


// 결과 패널에 보낼 데이터들 모아서 전달 (필요 시 계산)
public void SendResultData()
{
    // 설정했던 목표 녹화율
    // 녹화 적용된 건물 수
    // 녹화 적용 면적
    // 예상 감소 온도 (논문에서 가져온 기준이 되는 온도)
    // 녹화 전 기존 온도
    // 녹화 후 예상 감소 온도 (녹화 전 기존 온도 - 예상 감소 온도)
}





}
