#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

// ─────────────────────────────────────────────────────────────
// CSV(montly_temp_color_standard.csv)를 읽어 TempColorTable.asset 으로 굽는 에디터 도구.
//
//   메뉴:  Tools > UHI > Bake TempColorTable from CSV
//
// ※ 중요: 이 파일은 반드시 'Editor' 라는 이름의 폴더 안에 있어야 합니다.
//         (예: Assets/Scripts/Editor/TempColorTableImporter.cs)
//         #if UNITY_EDITOR 가드도 걸려 있어 빌드에는 포함되지 않습니다.
// ─────────────────────────────────────────────────────────────

public static class TempColorTableImporter
{
    [MenuItem("Tools/UHI/Bake TempColorTable from CSV")]
    public static void Bake()
    {
        // 1. 구울 CSV 파일 선택
        string csvPath = EditorUtility.OpenFilePanel(
            "온도-색상 기준 CSV 선택", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath)) return;

        // 2. 저장 위치(.asset) 선택 — 반드시 Assets 폴더 안
        string savePath = EditorUtility.SaveFilePanelInProject(
            "TempColorTable 저장 위치", "TempColorTable", "asset",
            "구워진 ScriptableObject를 저장할 위치를 선택하세요.");
        if (string.IsNullOrEmpty(savePath)) return;

        // 3. CSV 파싱
        var entries = ParseCsv(csvPath);
        if (entries.Count == 0)
        {
            Debug.LogError("[TempColorTableImporter] 파싱된 행이 0개입니다. CSV 형식을 확인하세요.");
            return;
        }

        // 4. 에셋 생성 또는 기존 에셋 갱신
        var table = AssetDatabase.LoadAssetAtPath<TempColorTable>(savePath);
        bool isNew = table == null;
        if (isNew) table = ScriptableObject.CreateInstance<TempColorTable>();

        table.entries = entries;
        table.InvalidateCache();

        if (isNew) AssetDatabase.CreateAsset(table, savePath);
        else EditorUtility.SetDirty(table);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TempColorTableImporter] 완료 — {entries.Count}개 행을 '{savePath}' 에 구웠습니다.");
        Selection.activeObject = table; // 프로젝트 창에서 선택해 보여줌
    }

    private static List<TempColorEntry> ParseCsv(string path)
    {
        var result = new List<TempColorEntry>();
        string[] lines = File.ReadAllLines(path); // BOM 은 .NET 이 자동 처리

        // 0번 줄(헤더) 건너뜀
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = line.Split(',');
            if (cols.Length < 5) continue;

            // 컬럼: month, level, temp_standard, season, color_code
            string monthStr = cols[0].Replace("월", "").Trim();   // "1월" -> "1"
            string levelStr = cols[1].Replace("단계", "").Trim();  // "1단계" -> "1"
            string tempStr  = cols[2].Trim();
            string hex      = cols[4].Trim();

            if (!int.TryParse(monthStr, out int month)) continue;
            if (!int.TryParse(levelStr, out int level)) continue;
            if (!float.TryParse(tempStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float temp)) continue;
            if (!ColorUtility.TryParseHtmlString(hex, out Color color)) continue;

            result.Add(new TempColorEntry
            {
                month = month,
                level = level,
                tempStandard = temp,
                color = color
            });
        }
        return result;
    }
}
#endif
