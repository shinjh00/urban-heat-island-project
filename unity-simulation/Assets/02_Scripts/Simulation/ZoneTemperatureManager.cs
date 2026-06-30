using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ZoneTemperatureManager : MonoBehaviour
{
    public List<ZoneData> zoneList = new List<ZoneData>();


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
        // NetworkManager의 FetchZoneFromApi() 코루틴 결과를 Task로 변환하기 위해 필요
        // IEnumerator나 콜백 기반 API는 async/await와 직접 연결할 수 없으므로
        // 이렇게 Task 상자를 만들고 파싱 및 할당 작업이 끝나면 상자를 닫는다는 개념
        TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

        StartCoroutine(NetworkManager.Instance.FetchZoneFromApi((rawJson) =>
        {
            tcs.SetResult(rawJson);
        }));

        // 통신이 완료될 때까지 비동기로 대기
        string rawJsonText = await tcs.Task;

        if (string.IsNullOrEmpty(rawJsonText))
        {
            Debug.LogError("[ZoneTemperatureManager] 서버로부터 받은 데이터가 없습니다.");
            return;
        }

        // 받아온 rawJsonText를 파싱하고 객체화
        DataParser parser = new DataParser();

        List<JObject> features = await Task.Run(() =>
        {
            List<JObject> list = new List<JObject>();
            JObject root = JObject.Parse(rawJsonText);
            foreach (JToken feat in (JArray)root["features"])
            {
                list.Add((JObject)feat);
            }
            return list;
        });

        // Data 추출 함수를 통해 객체화
        zoneList = parser.ExtractZoneData(features);

        if (zoneList != null && zoneList.Count > 0)
        {
            Debug.Log($"[ZoneTemperatureManager] Zone {zoneList.Count}개 로드 완료");
            Debug.Log($"[ZoneTemperatureManager] Zone - {zoneList[0].zoneId}, {zoneList[0].polygon}, {zoneList[0].temperature}");
        }
        else
        {
            Debug.LogWarning("[ZoneTemperatureManager] 로드된 데이터가 없습니다.");
        }
        
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
