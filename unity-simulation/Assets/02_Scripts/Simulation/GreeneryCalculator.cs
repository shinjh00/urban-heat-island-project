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

    //리스트 추출 함수모음



    //높은 점수 순서대로 리스트 제작
    public void AscendingScoreList()
    {

    }

    //각 건물 ID에 녹화 점수 할당
    public void AllocateBuildingScore()
    {

    }

    // Top10 건물 리스트 제작
    public void Top10ScoreList()
    {
        // AscendingScoreList에서 만든거에서 앞에 10개 추출
    }

    // 녹화 건물 수 카운트 제작
    public void CountingGreenaryBuilding()
    {
        //녹화된 건물 수 세기 enum 으로 카운트
        //enum 에서 selted 값 + 10 (탑텐수)
        
        //반환값은 int

        //이 값을 나중에 controller가 ui 결과 패널에 전달할거임
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
    /// 특정 구역(zoneID)에 속한 건물들의 총 면적(AreaSize)을 계산하여 반환합니다.
    /// </summary>
    /// <param name="targetZoneBuildings">필터링된 해당 구역의 건물 리스트</param>
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



    //리스트 토대로 차례대로 목표 녹화 면적 까지 더함
    public void CalcUntilGreeneryGoal()
    {
        // AscendingScoreList에서 만든 리스트 순회하며
        // 목표 녹화 면적에 도달할 때까지 누적 더하기 +=
        //목표 녹화 면적 충족시 계산 과정 종료 (반복문, 코루틴 아마 사용해야할듯?)
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


    // 녹화 전후 온도 감소 정도 계산
    public void CalcBeforeAfterGreenery()
    {
        // <파라미터값>
        // 녹화 전 기존 온도
        // CalcExpectedMinusTemp 에서 추출한 내려가는 값

        // <return값>
        // 녹화 후 예상 감소 온도 (녹화 전 기존 온도 - CalcExpectedMinusTemp 추출한 예상 감소 온도)

        // => 목표 녹화율 높을수록 감소 온도 커짐
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
