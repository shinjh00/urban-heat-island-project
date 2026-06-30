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
        CalculationResultData result = new CalculationResultData();
        result.buildingScores = new Dictionary<string, float>();
        result.radiusCounts = new Dictionary<string, int>();

        // 1. 기본 면적 및 온도 효과 수식 처리
        result.totalAreaSize = CalcTotalAreaSize(request.targetZoneBuildings);
        result.expectedGreeneryArea = CalcTotalGreeneryArea(result.totalAreaSize, request.greeneryRate);
        result.expectedTempEffect = CalcExpectedTempEffect(request.greeneryRate);

        // 2. 루프를 돌며 각 빌딩별 개별 점수 및 반경 연산 진행
        foreach (var bSimData in request.targetZoneBuildings)
        {
            int radiusCount = CalcRadiusBuildingCount(bSimData, request.allBuildings);
            float score = CalcGreeneryScore(radiusCount, bSimData.areaSize, bSimData.height);

            // 전달용 사전에 ID 기반으로 결과 저장
            result.radiusCounts[bSimData.buildingID] = radiusCount;
            result.buildingScores[bSimData.buildingID] = score;
        }

        // 3. [이벤트 발행] 연산이 완료된 최종 바구니를 전방으로 발사합니다.
        OnCalculationCompleted?.Invoke(result);
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
        float expectedGreeneryArea = totalAreaSize * (currentGreeneryRate / 100f);

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
        float expectedTempEffect = MaxExpectedTempEffect / 100 * greeneryRate;

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




}
