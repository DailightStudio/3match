using UnityEngine;
using System.Collections.Generic;


/// <summary
/// 3축(가로/대각) 양방향 매칭 + 폭탄 전용 라인(색 무시)
/// </summary>
public class MatchService
{
    readonly GridService grid;
    public MatchService(GridService grid) { this.grid = grid; }

    public List<List<Vector2Int>> FindMatchesAt(Vector2Int a, Vector2Int b)
    {
        var results = new List<List<Vector2Int>>();
        AddUnique(results, FindFrom(a.x, a.y));
        AddUnique(results, FindFrom(b.x, b.y));
        return results;
    }

    public List<List<Vector2Int>> FindAll()
    {
        var results = new List<List<Vector2Int>>();
        for (int y = 0; y < grid.H; y++)
            for (int x = 0; x < grid.W; x++)
                AddUnique(results, FindFrom(x, y));
        return results;
    }

    void AddUnique(List<List<Vector2Int>> dst, List<List<Vector2Int>> src)
    {
        foreach (var g in src)
        {
            bool dup = false;
            foreach (var h in dst)
            {
                if (g.Count == h.Count)
                {
                    int same = 0;
                    for (int i = 0; i < g.Count; i++) if (h.Contains(g[i])) same++;
                    if (same == g.Count) { dup = true; break; }
                }
            }
            if (!dup) dst.Add(g);
        }
    }

    List<List<Vector2Int>> FindFrom(int sx, int sy)
    {
        var results = new List<List<Vector2Int>>();
        if (!grid.Inside(sx, sy)) return results;
        var start = grid.Get(sx, sy);
        if (start == null) return results;

        // 같은 타입 3+
        var g1 = CollectBidirectionalAxial(sx, sy, 0, 3, b => b.type == start.type);
        var g2 = CollectBidirectionalAxial(sx, sy, 5, 2, b => b.type == start.type);
        var g3 = CollectBidirectionalAxial(sx, sy, 4, 1, b => b.type == start.type);
        if (g1.Count >= 3) results.Add(g1);
        if (g2.Count >= 3) results.Add(g2);
        if (g3.Count >= 3) results.Add(g3);

        // 폭탄만 3+ (색 무시)
        if (start.isBomb)
        {
            var b1 = CollectBidirectionalAxial(sx, sy, 0, 3, b => b.isBomb);
            var b2 = CollectBidirectionalAxial(sx, sy, 5, 2, b => b.isBomb);
            var b3 = CollectBidirectionalAxial(sx, sy, 4, 1, b => b.isBomb);
            if (b1.Count >= 3) results.Add(b1);
            if (b2.Count >= 3) results.Add(b2);
            if (b3.Count >= 3) results.Add(b3);
        }
        return results;
    }

    // HexCoords.Neighbor: 0=E,1=NE,2=NW,3=W,4=SW,5=SE
    List<Vector2Int> CollectBidirectionalAxial(int sx, int sy, int dirFwd, int dirBack, System.Func<Block, bool> ok)
    {
        var run = new List<Vector2Int> { new Vector2Int(sx, sy) };
        var aSeed = HexCoords.OffsetOddRToAxial(sx, sy);

        var a = aSeed;
        while (true)
        {
            a = HexCoords.Neighbor(a, dirFwd);
            var off = HexCoords.AxialToOffsetOddR(a.q, a.r);
            if (!grid.Inside(off.x, off.y)) break;
            var nb = grid.Get(off.x, off.y);
            if (nb == null || !ok(nb)) break;
            run.Add(off);
        }
        a = aSeed;
        while (true)
        {
            a = HexCoords.Neighbor(a, dirBack);
            var off = HexCoords.AxialToOffsetOddR(a.q, a.r);
            if (!grid.Inside(off.x, off.y)) break;
            var nb = grid.Get(off.x, off.y);
            if (nb == null || !ok(nb)) break;
            run.Insert(0, off);
        }
        return run;
    }
}
