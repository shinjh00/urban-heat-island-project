using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;


// TODO: [피드백] 지금 이 enum을 실제로 쓰는 곳이 없음
// 그리너리 관련된 정보가 현재는 나눠져 있음. 
//BuildingData.cs에 만들어진 GreeneryState enum (Normal, Selected, top10Selected) 쓰이는 곳 없음
//BuildingInfo.cs에서는 greeneryScore(float), greeneryRank(int) 점수/순위만 있고 상태 없음

//SimulationController.cs에서는 greeneryStatus가 그냥 문자열("GreeneryPriority", "GreeneryTop10") 
//문자열을 사용하는 것은 위험하니 아래 enum을 전체적용하자

//BuildingInfo에는 enum 변수 추가하여 수정. SimulationController에서도 enum으로 수정. 
//GreeneryCalculator에서는 문자열 대신 enum값 직접세팅.  

//enum을 사용하면 jsonconvert 쓸때 StringEnumConverter만 붙여도 "GreeneryTop10"처럼 사람이 읽기 좋은 형태로 사용도 가능함! 
//public enum GreeneryState
//{
//    Normal = 0,
//    GreeneryPriority = 1,
//    GreeneryTop10 = 2
//}

// 건물 녹화 상태
public enum GreeneryState
{
    Normal = 0,             // 기본 상태
    GreeneryPriority = 1,   // 녹화 대상 선정 (top10 제외)
    GreeneryTop10 = 2       // 녹화 대상 중 top10 선정
}

// 건물 데이터 모델
[Serializable]
public class BuildingData
{
    public string id;               // 원천도형ID
    public string dongCode;         // 행정동코드
    public string address;          // 주소 (동까지)
    public string addressDetail;    // 주소 (지번)
    public string type;             // 용도
    public string material;         // 구조재
    public float areaSize;          // 면적  ** 시뮬 사용 **
    public float height;            // 높이  ** 시뮬 사용 **
    public int floors;              // 층수
    public double lat;              // 위도 (GeoJSON coordinates - y값)
    public double lon;              // 경도 (GeoJSON coordinates - x값)
    public List<Vector2> polygon;   // 건물 외곽선 꼭짓점들의 로컬 xz 좌표 목록
}
