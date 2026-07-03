using UnityEngine;

// 빌딩 GameObject 에 붙일 컴포넌트로, BuildingData 를 씬과 연결하는 역할.
// 로직 없이 데이터만 보관한다.
// BuildingSelector 나 BuildingManager 에서 GetComponent<BuildingInfo>() 로 접근한다.
public class BuildingInfo : MonoBehaviour
{
    public BuildingData data;

    // Zone Id 값
    public string zoneId;

    // Zone 온도값
    public float temperature;

    // 반경 0m 내에 있는 건물 수
    public int radiusBuildingCount;

    // TODO: [피드백] BuildingData.cs 피드백 같이 참고하여 수정하기
    public float greeneryScore;


    // 옥상 녹화 우선순위
    public int greeneryRank;
}