using System;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────
// 월별 × 15단계 온도-색상 기준표를 담는 ScriptableObject.
// CSV(montly_temp_color_standard.csv)를 에디터에서 .asset 으로 "구워서" 사용한다.
// 런타임에는 GetColor(month, temp) 로 특정 월·온도에 해당하는 색을 조회한다.
//
// CSV 컬럼:  month(1월~12월), level(1단계~15단계), temp_standard, season, color_code(#hex)
// ─────────────────────────────────────────────────────────────

// CSV 한 줄에 대응하는 데이터 한 칸.
[Serializable]
public class TempColorEntry
{
    public int month;          // 1~12
    public int level;          // 1~15 (단계)
    public float tempStandard; // 해당 단계의 기준 온도(하한값으로 사용)
    public Color color;        // 표시 색상
}

[CreateAssetMenu(fileName = "TempColorTable", menuName = "UHI/Temp Color Table")]
public class TempColorTable : ScriptableObject
{
    [Tooltip("CSV에서 구워진 전체 (월, 단계, 기준온도, 색상) 목록")]
    public List<TempColorEntry> entries = new List<TempColorEntry>();

    // 조회 속도를 위해 월별로 미리 정렬해 캐싱한다. (월 -> 단계 오름차순 리스트)
    private Dictionary<int, List<TempColorEntry>> _byMonth;

    private void BuildCache()
    {
        _byMonth = new Dictionary<int, List<TempColorEntry>>();
        foreach (var e in entries)
        {
            if (!_byMonth.TryGetValue(e.month, out var list))
            {
                list = new List<TempColorEntry>();
                _byMonth[e.month] = list;
            }
            list.Add(e);
        }
        // 단계 오름차순 정렬 (= 기준온도 오름차순)
        foreach (var kv in _byMonth)
            kv.Value.Sort((a, b) => a.level.CompareTo(b.level));
    }

    /// <summary>
    /// 특정 월(1~12)과 온도에 해당하는 색을 반환한다.
    /// 각 단계의 기준온도를 "하한"으로 보고,
    /// temp 이하인 단계 중 가장 높은 단계의 색을 고른다.
    /// (temp 가 1단계보다 낮으면 1단계 색, 15단계보다 높으면 15단계 색)
    /// </summary>
    public Color GetColor(int month, float temp)
    {
        if (_byMonth == null) BuildCache();

        if (!_byMonth.TryGetValue(month, out var list) || list.Count == 0)
        {
            Debug.LogWarning($"[TempColorTable] {month}월 데이터가 없습니다. 회색을 반환합니다.");
            return Color.gray;
        }

        Color result = list[0].color;          // 기본: 가장 낮은 단계 색
        foreach (var e in list)
        {
            if (temp >= e.tempStandard) result = e.color; // 기준 넘으면 그 색으로 갱신
            else break;                                   // 정렬돼 있으므로 더 볼 필요 없음
        }
        return result;
    }

#if UNITY_EDITOR
    // 에디터에서 entries 를 다시 채울 때 캐시를 무효화한다.
    public void InvalidateCache() => _byMonth = null;
#endif
}
