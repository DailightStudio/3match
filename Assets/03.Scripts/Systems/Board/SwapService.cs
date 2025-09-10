using UnityEngine;
using System.Collections;


/// <summary>인접 판정/스왑/롤백</summary>
public class SwapService
{
    readonly GridService grid;

    public SwapService(GridService grid) { this.grid = grid; }

    public bool AreAdjacent(int x1, int y1, int x2, int y2)
    {
        if (!grid.Inside(x1, y1) || !grid.Inside(x2, y2)) return false;
        if (x1 == x2 && y1 == y2) return false;

        var a = HexCoords.OffsetOddRToAxial(x1, y1);
        for (int dir = 0; dir < 6; dir++)
        {
            var n = HexCoords.Neighbor(a, dir);
            var off = HexCoords.AxialToOffsetOddR(n.q, n.r);
            if (off.x == x2 && off.y == y2) return true;
        }
        return false;
    }

    public IEnumerator SwapOnce(int x1, int y1, int x2, int y2, float speed)
    {
        var a = grid.Get(x1, y1);
        var b = grid.Get(x2, y2);
        if (!a || !b) yield break;

        grid.Set(x1, y1, b); b.SetGridPos(x1, y1);
        grid.Set(x2, y2, a); a.SetGridPos(x2, y2);

        a.MoveTo(x2, y2, speed);
        b.MoveTo(x1, y1, speed);

        while (a.IsMoving || b.IsMoving) yield return null;
        yield return null;
    }
}
