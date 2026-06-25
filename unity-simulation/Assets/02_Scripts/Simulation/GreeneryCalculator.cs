using UnityEngine;

public class GreeneryCalculator : MonoBehaviour
{


    //리스트 추출 함수모음
    


    //높은 점수 순서대로 리스트 제작
    public void AscendingScoreList()
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

    //반경 내 건물 수 계산
    public void CalcRadiusBuildingCount()
    {
        // 건물 별 중심점으로부터 250m 반경 내 건물 수
    }


    //격자 내부에 있는 건물들 점수 계산
    public void CalcGreeneryScore()
    {
        // 우선순위 함수 적용
        // 계산식 사용
        // 반경 내 건물 수, 높이, 면적 값 필요
    }


    // 목표 녹화율에 따른 목표 녹화 면적 계산
    public void CalcTotalGreeneryArea()
    {
        //그리드 내에 있는 모든 건물의 옥상 면적 합
        //이 합에다가 목표 녹화율 곱해서 목표 녹화 면적 계산
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
    public void CalcExpectedMinusTemp()
    {
        // 논문에서 근거한 온도 정보 계산
        // 목표 녹화율에 따른 예상 감소 온도 제작
        // 녹화율 높을수록 낮은 값 내려감
        // 내려가는 값 리턴
        // 결과값은 float 값 (4도~6도)
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





}
