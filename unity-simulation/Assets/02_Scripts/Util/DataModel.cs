using System;
using System.Collections.Generic;
using UnityEngine;


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


// S-DoT 센서 데이터 모델
//      SN	        TIME	      AVG_TP  MAX_TP	    address	                   lat     	   lon
// V02Q1941006  2026-06-21 23:07    25     25.1    서울특별시 마포구 포은로6길 10	37.5552571	126.9042671
[Serializable]
public class SDotSensorData
{
    public string serialNum;    // 센서 고유 시리얼 넘버
    public int year;            // 날짜 - 년
    public int month;           // 날짜 - 월
    public int day;             // 날짜 - 일
    public float averageTemp;   // 평균 온도
    public float maxTemp;       // 최고 온도
    public string address;      // 센서 위치 주소
    public float lat;           // 위도
    public float lon;           // 경도
}