using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using static SimulationController;

public class GreeneryCalculator : MonoBehaviour
{

    [Header("Greenery Calculation Settings")]
    public double searchRadiusMeters = 250.0; // 반경 설정 (단위: 미터)
    private float MaxExpectedTempEffect = 0.42f;

    //Controller에게 연산이 완료되었음을 알리는 이벤트
    public static event Action<CalculationResultData> OnCalculationCompleted;

    private void Start()
    {
        // [구독] Controller가 요청하는 이벤트를 항시 대기합니다.
        SimulationController.OnCalculationRequested += ExecuteCalculation;
    }

    private void OnDestroy()
    {
        //[구독 해제]
        SimulationController.OnCalculationRequested -= ExecuteCalculation;
    }


    /// <summary>
    /// Controller가 던진 데이터를 받아 무겁고 복잡한 수식 연산 처리를 전담하는 함수
    /// </summary>
    private void ExecuteCalculation(CalculationRequestData request)
    {
        // 연산 진입 확인
        Debug.Log($"[GreeneryCalculator] 연산 요청 수신! 타겟 구역 건물 수: {request.targetZoneBuildings.Count}개," +
            $" 전체 건물 수: {request.allBuildings.Count}개");

        CalculationResultData result = new CalculationResultData();
        result.buildingScores = new Dictionary<string, float>();
        result.radiusCounts = new Dictionary<string, int>();

        // 1. 기본 면적 및 온도 효과 수식 처리
        result.totalAreaSize = CalcTotalAreaSize(request.targetZoneBuildings);
        result.expectedGreeneryArea = CalcTotalGreeneryArea(result.totalAreaSize, request.greeneryRate);
        result.expectedTempEffect = CalcExpectedTempEffect(request.greeneryRate);

        // 1단계 연산 결과 출력
        Debug.Log($"[GreeneryCalculator] 1단계 완료 | 총 구역 면적: {result.totalAreaSize}㎡," +
            $" 목표 녹화 면적: {result.expectedGreeneryArea}㎡, 예상 감소 온도: {result.expectedTempEffect}°C");

        // 2. 루프를 돌며 각 빌딩별 개별 점수 및 반경 연산 진행
        foreach (var bSimData in request.targetZoneBuildings)
        {
            // 점수에 필요한 건물당 인접 건물 수 계산
            int radiusCount = CalcRadiusBuildingCount(bSimData, request.allBuildings);
            // 건물별 Greenery Score 점수 계산 
            float score = CalcGreeneryScore(radiusCount, bSimData.areaSize, bSimData.height);

            // 전달용 사전에 ID 기반으로 결과 저장
            result.radiusCounts[bSimData.buildingID] = radiusCount;
            result.buildingScores[bSimData.buildingID] = score;
        }

        // [로그 추가] 2단계 연산 결과 확인
        Debug.Log($"[GreeneryCalculator] 2단계 완료 | {request.targetZoneBuildings.Count}개 " +
            $"건물의 개별 점수 및 반경 연산 완료.");


        // 3. 점수 기반 랭킹 산출 + 누적 면적 기준 녹화 상태(GreeneryPriority / GreeneryTop10) 판정
        result.rankedBuildings = CalcGreeneryRanking(
            request.targetZoneBuildings,
            result.buildingScores,
            result.expectedGreeneryArea
        );

        // 4. [이벤트 발행] 연산이 완료된 최종 바구니를 전방으로 발사합니다.
        OnCalculationCompleted?.Invoke(result);

        Debug.Log($"[GreeneryCalculator] 모든 연산 성공! 최종 Greenery Ranking 리스트를 Controller로 발송합니다.");
    }


    // 계산 함수 모음


    //격자 내부에 있는 건물들 점수 계산후 점수 출력
    public float CalcGreeneryScore(int radiusBuildingCount, float areaSize, float height)
    {
        // 우선순위 함수 및 계산식 적용
        // 반경 내 건물 수, 높이, 면적 값을 활용한 임의의 수식 예시
        float score = (radiusBuildingCount * 0.5f) + (areaSize * 0.3f) + (height * 0.2f);

        // 계산된 점수를 float 형태로 반환합니다.
        return score;
    }


    //반경 내 건물 수 계산
    public int CalcRadiusBuildingCount(
        SimulationController.SpawnedAllBuildings targetBuilding, 
        List<SimulationController.SpawnedAllBuildings> allBuildings
        )
    {
        int count = 0;

        foreach (var otherBuilding in allBuildings)
        {
            // 자기 자신은 카운트에서 제외합니다.
            if (targetBuilding.buildingID == otherBuilding.buildingID) continue;

            // 두 건물 중앙 좌표 간의 거리 계산 (하버사인 공식 사용)
            double distance = GetDistanceInMeters(
                targetBuilding.buildingCenterLat, targetBuilding.buildingCenterLon,
                otherBuilding.buildingCenterLat, otherBuilding.buildingCenterLon
            );

            // 계산된 거리가 설정한 반경 이내라면 카운트 증가
            if (distance <= searchRadiusMeters)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 특정 구역(zoneID)에 속한 건물들의 총 면적(AreaSize)을 계산하여 반환
    /// </summary>
    public float CalcTotalAreaSize(List<SpawnedAllBuildings> targetZoneBuildings)
    {
        // 리스트가 비어있다면 0을 반환
        if (targetZoneBuildings == null || targetZoneBuildings.Count == 0)
            return 0f;

        // 해당 구역 모든 건물의 areaSize를 더해서 반환
        return targetZoneBuildings.Sum(b => b.areaSize);
    }


    // 목표 녹화율에 따른 목표 녹화 면적 계산
    public float CalcTotalGreeneryArea(float totalAreaSize, float currentGreeneryRate)
    {
        float expectedGreeneryArea = totalAreaSize * (currentGreeneryRate);

        return expectedGreeneryArea;
    }


    // 온도 관련 함수모음

    // 예상 감소 온도(논문) 추출
    public float CalcExpectedTempEffect(float greeneryRate)
    {
        // 논문에서 근거한 온도 정보 계산
        // 목표 녹화율에 따른 예상 감소 온도 제작
        // 녹화율 높을수록 낮은 값 내려감
        // 내려가는 값 리턴
        float expectedTempEffect = MaxExpectedTempEffect * greeneryRate;

        return expectedTempEffect;
    }

    /// <summary>
    /// 건물별 중앙값을 기준으로 거리 확인 위해서 하버사인 공식을 이용해 위도/경도 좌표 간의 직선 거리(미터)를 계산합니다.
    /// </summary>
    private double GetDistanceInMeters(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371000; // 지구 반지름 (미터 단위)

        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double val)
    {
        return (Math.PI / 180) * val;
    }

    /// <summary>
    /// 녹화 점수를 기준으로 순위(greeneryRank)를 매기고,
    /// 순위가 높은 순서대로 areaSize를 누적하여 목표 녹화 면적(expectedGreeneryArea)에
    /// 도달할 때까지 "GreeneryPriority" 상태를 부여합니다.
    /// 마지막으로 순위 1~10위는 "GreeneryTop10"으로 재지정합니다.
    /// </summary>
    public List<SimulationController.GreeneryRankingBuildings> CalcGreeneryRanking(
        List<SimulationController.SpawnedAllBuildings> targetZoneBuildings,
        Dictionary<string, float> buildingScores,
        float expectedGreeneryArea)
    {
        // 1. GreeneryRankingBuildings 리스트 조립 (기본 상태: Normal)
        List<SimulationController.GreeneryRankingBuildings> zoneBuildings = new List<SimulationController.GreeneryRankingBuildings>();

        foreach (var bSimData in targetZoneBuildings)
        {
            SimulationController.GreeneryRankingBuildings gData = new SimulationController.GreeneryRankingBuildings();
            gData.buildingID = bSimData.buildingID;
            gData.areaSize = bSimData.areaSize;

            if (buildingScores.ContainsKey(bSimData.buildingID))
                gData.greeneryScore = buildingScores[bSimData.buildingID];

            gData.greeneryStatus = GreeneryState.Normal; // 기존 string에서 Enum으로 매핑 변경;
            gData.greeneryRank = 0;

            zoneBuildings.Add(gData);
        }

        // 2. 점수 내림차순 정렬 후 순위(greeneryRank) 부여
        List<SimulationController.GreeneryRankingBuildings> rankedZoneBuildings =
            zoneBuildings.OrderByDescending(b => b.greeneryScore).ToList();

        for (int i = 0; i < rankedZoneBuildings.Count; i++)
        {
            SimulationController.GreeneryRankingBuildings updatedData = rankedZoneBuildings[i];
            updatedData.greeneryRank = i + 1;
            rankedZoneBuildings[i] = updatedData;
        }

        // 3. 순위 순서대로 areaSize를 누적하며 목표 녹화 면적(expectedGreeneryArea)에
        //    도달할 때까지 GreeneryPriority 상태 부여
        float accumulatedArea = 0f;
        int priorityAssignCount = 0; // [로그용 변수 추가]

        for (int i = 0; i < rankedZoneBuildings.Count; i++)
        {
            if (accumulatedArea >= expectedGreeneryArea) break;

            SimulationController.GreeneryRankingBuildings updatedData = rankedZoneBuildings[i];
            accumulatedArea += updatedData.areaSize;
            updatedData.greeneryStatus = GreeneryState.GreeneryPriority; // Enum 대입
            rankedZoneBuildings[i] = updatedData;
            priorityAssignCount++;
        }

        // 면적 누적 결과 리포트
        Debug.Log($"[GreeneryCalculator] 랭킹 가공 중 | 목표 면적({expectedGreeneryArea}㎡) " +
            $"대비 누적 면적: {accumulatedArea}㎡ (Priority 부여 건물 수: {priorityAssignCount}개)");

        // 4. 순위 1~10위는 GreeneryPriority 대신 GreeneryTop10으로 재지정
        for (int i = 0; i < rankedZoneBuildings.Count; i++)
        {
            SimulationController.GreeneryRankingBuildings updatedData = rankedZoneBuildings[i];

            if (updatedData.greeneryRank >= 1 && updatedData.greeneryRank <= 10)
            {
                updatedData.greeneryStatus = GreeneryState.GreeneryTop10; // Enum 대입
                rankedZoneBuildings[i] = updatedData;
            }
        }

        return rankedZoneBuildings;
    }


}
