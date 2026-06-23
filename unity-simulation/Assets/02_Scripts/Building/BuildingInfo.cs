using UnityEngine;

// 빌딩 GameObject 에 붙일 컴포넌트로, BuildingData 를 씬과 연결하는 역할.
// 로직 없이 데이터만 보관한다.
// BuildingSelector 나 BuildingManager 에서 GetComponent<BuildingInfo>() 로 접근한다.
public class BuildingInfo : MonoBehaviour
{
    public BuildingData data;
}