using UnityEngine;

/// <summary>
/// 헥사(odd-r offset) 좌표 <-> 축좌표(axial) 변환 및 월드 좌표 유틸
/// </summary>
public static class HexCoords
{
    public struct Axial { public int q; public int r; public Axial(int q, int r){ this.q=q; this.r=r; } }

    // odd-r offset (x,y) -> axial(q,r)
    public static Axial OffsetOddRToAxial(int x, int y)
    {
        int q = x - (y - (y & 1)) / 2;
        int r = y;
        return new Axial(q, r);
    }

    // axial(q,r) -> odd-r offset(x,y)
    public static Vector2Int AxialToOffsetOddR(int q, int r)
    {
        int x = q + (r - (r & 1)) / 2;
        int y = r;
        return new Vector2Int(x, y);
    }

    // 6방향 축좌표 이웃
    static readonly Axial[] dirs = new Axial[] {
        new Axial(+1, 0), new Axial(+1, -1), new Axial(0, -1),
        new Axial(-1, 0), new Axial(-1, +1), new Axial(0, +1)
    };
    public static Axial Neighbor(Axial a, int dir)
    {
        Axial d = dirs[dir % 6];
        return new Axial(a.q + d.q, a.r + d.r);
    }

    // odd-r 그리드 -> 월드
    public static Vector3 GridToWorld(int x, int y, float cellSize)
    {
        float xOffset = (y % 2 == 1) ? 0.5f * cellSize : 0f;
        float wx = (x * cellSize) + xOffset;
        float wy = -y * (cellSize * 0.8660254f); // sin(60°)
        return new Vector3(wx, wy, 0f);
    }
}
