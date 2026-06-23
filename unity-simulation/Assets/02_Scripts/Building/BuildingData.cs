using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

// 추후 필요할 경우 사용 (enum 아닌 방법이 더 나으면 이거 안 써도됨)
public enum BuildingState
{
    Normal,     // 기본 상태
    Selected,   // 시뮬레이션 중심 건물로 선택된 상태
    Included    // 시뮬레이션 반경 내에 포함된 상태
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
    public float areaSize;          // 면적
    public float height;            // 높이
    public int floors;              // 층수
    public double lat;              // 위도 (GeoJSON coordinates - y값)
    public double lon;              // 경도 (GeoJSON coordinates - x값)
    public List<Vector2> polygon;   // 건물 외곽선 꼭짓점들의 로컬 xz 좌표 목록

}
