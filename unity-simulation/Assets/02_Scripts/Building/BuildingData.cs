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



