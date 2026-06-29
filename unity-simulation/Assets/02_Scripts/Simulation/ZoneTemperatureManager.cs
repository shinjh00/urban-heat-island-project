using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ZoneTemperatureManager : MonoBehaviour
{
    public List<ZoneData> zoneList = new List<ZoneData>();

    public Material gridMaterial;

    // <원래 들어가야 할 코드>
    //public async Task ~~ () { }
    // Flask API 통신 (UnityWebRequest 사용)
    // 응답받은 JSON을 파싱하여 zoneList에 저장


    private async void Start()
    {
        await FetchZoneData();
        DrawGrid(zoneList);
    }

    // 테스트용 코드
    // SimulationController에서 사용
    public async Task FetchZoneData()
    {
        DataParser parser = new DataParser();

        // 파일을 파싱해서 JObject 리스트로 가져옴
        List<JObject> features = await parser.ParseGeoJson("grid.geojson");

        // Data 추출 함수를 통해 객체화
        zoneList = parser.ExtractZoneData(features);

        Debug.Log($"[ZoneTemperatureManager] Zone {zoneList.Count}개 로드 완료");
        Debug.Log($"[ZoneTemperatureManager] Zone - {zoneList[0].zoneId}, {zoneList[0].polygon}, {zoneList[0].temperature}");
    }


    public void DrawGrid(List<ZoneData> zoneList)
    {
        foreach (ZoneData zone in zoneList)
        {
            // 그리드 오브젝트 생성
            GameObject gridObj = new GameObject("Zone_" + zone.zoneId);
            gridObj.transform.SetParent(this.transform);
        }
    }

}
