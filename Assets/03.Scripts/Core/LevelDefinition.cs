using System;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class LevelDefinition
{
    // JSON: width/height를 안 넣어도 cells로부터 자동 계산해줌
    [JsonProperty("width")] public int width;
    [JsonProperty("height")] public int height;

    // 2차원(가변) 배열: 각 행이 한 줄
    [JsonProperty("cells")] public int[][] cells;

    /// <summary>
    /// (x,y)에서 타입 반환. 범위 밖이면 -1(막힘) 처리.
    /// -1 = 막힌 칸 / 0~N = 블록 타입 인덱스
    /// </summary>
    public int Get(int x, int y)
    {
        if (cells == null || cells.Length == 0) return -1;
        if (y < 0 || y >= cells.Length) return -1;
        var row = cells[y];
        if (row == null || x < 0 || x >= row.Length) return -1;
        return row[x];
    }

    /// <summary>JSON 문자열을 뉴턴으로 역직렬화 + 유효성 검증</summary>
    public static LevelDefinition FromJson(string json)
    {
        try
        {
            var lvl = JsonConvert.DeserializeObject<LevelDefinition>(json);

            if (lvl == null || lvl.cells == null || lvl.cells.Length == 0)
            {
                Debug.LogError("cells가 비어있습니다.");
                return null;
            }

            // width/height 자동 보정
            lvl.height = (lvl.height > 0) ? lvl.height : lvl.cells.Length;
            lvl.width = (lvl.width > 0) ? lvl.width : (lvl.cells[0]?.Length ?? 0);

            // 행 길이 일관성 체크
            for (int y = 0; y < lvl.height; y++)
            {
                var row = lvl.cells[y];
                if (row == null || row.Length != lvl.width)
                {
                    Debug.LogError($"행 {y}의 길이({row?.Length})가 width({lvl.width})와 다릅니다.");
                    return null;
                }
            }

            return lvl;
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON 파싱 실패: {e.Message}");
            return null;
        }
    }
}
