using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 보드 데이터/좌표계 관리(스폰/제거/이동/보충/경계계산/이웃)
/// </summary>
public class GridService
{
    public int W { get; private set; }
    public int H { get; private set; }
    public bool IsReady => blocks != null;

    Block[,] blocks;
    bool[,] blocked;               // 막힌 칸 지원 (-1 등)
    Transform root;
    float cellSize;
    Vector3 origin;
    Block prefab;
    Sprite[] blockSprites;

    /// <summary>현재 로컬 원점(WorldPos 계산 시 더해지는 오프셋)</summary>
    public Vector3 Origin => origin;

    public GridService(Transform rootTransform) { root = rootTransform; }

    public void Configure(int width, int height, float cellSize, Vector3 origin, Block prefab, Sprite[] blockSprites)
    {
        this.W = width; this.H = height;
        this.cellSize = cellSize; this.origin = origin;
        this.prefab = prefab; this.blockSprites = blockSprites;
        blocks = new Block[W, H];
        blocked = new bool[W, H]; // 기본 false
    }

    // ===== Blocked / Inside =====
    public void SetBlocked(int x, int y, bool value)
    {
        if (x >= 0 && x < W && y >= 0 && y < H) blocked[x, y] = value;
    }

    public bool IsBlocked(int x, int y)
    {
        if (x < 0 || x >= W || y < 0 || y >= H) return true;
        return blocked != null && blocked[x, y];
    }

    public bool Inside(int x, int y)
        => (x >= 0 && x < W && y >= 0 && y < H && (blocked == null || !blocked[x, y]));

    // ===== Accessors =====
    public Block Get(int x, int y) => Inside(x, y) ? blocks[x, y] : null;
    public void Set(int x, int y, Block b) { if (Inside(x,y)) blocks[x, y] = b; }

    // ===== Coordinate helpers =====
    public Vector3 WorldPos(int x, int y)
    {
        var local = HexCoords.GridToWorld(x, y, cellSize);
        return root.TransformPoint(origin + local);
    }

    // ===== Spawn / Remove / Move =====
    public Block Spawn(BlockType type, int x, int y, BoardManager board)
    {
        if (!Inside(x, y) || !prefab) return null;
        var go = UnityEngine.Object.Instantiate(prefab, WorldPos(x, y), Quaternion.identity, root);
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr && blockSprites != null && blockSprites.Length > (int)type) sr.sprite = blockSprites[(int)type];
        go.Init(board, x, y, type);
        go.ApplyBombSprite(false, null);
        blocks[x, y] = go;
        return go;
    }

    public void Remove(int x, int y)
    {
        var b = Get(x, y);
        if (!b) return;
        blocks[x, y] = null;
        UnityEngine.Object.Destroy(b.gameObject);
    }

    public void MoveBlock(Block b, int nx, int ny, float fallSpeed)
    {
        blocks[b.X, b.Y] = null;
        blocks[nx, ny] = b;
        b.MoveTo(nx, ny, fallSpeed);
    }

    // ===== Layout helpers =====
    public void RefreshAllWorldPositions()
    {
        if (blocks == null) return;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (blocks[x, y] != null)
                    blocks[x, y].transform.position = WorldPos(x, y);
    }

    public void GetWorldBounds(out Vector3 minW, out Vector3 maxW)
    {
        minW = new Vector3(float.PositiveInfinity, float.PositiveInfinity, 0f);
        maxW = new Vector3(float.NegativeInfinity, float.NegativeInfinity, 0f);
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            if (IsBlocked(x, y)) continue;
            var p = WorldPos(x, y);
            if (p.x < minW.x) minW.x = p.x; if (p.y < minW.y) minW.y = p.y;
            if (p.x > maxW.x) maxW.x = p.x; if (p.y > maxW.y) maxW.y = p.y;
        }
    }

    public void CenterByCurrentBounds()
    {
        if (W <= 0 || H <= 0) return;
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                if (IsBlocked(x, y)) continue;
                Vector3 w = HexCoords.GridToWorld(x, y, cellSize);
                if (w.x < min.x) min.x = w.x; if (w.y < min.y) min.y = w.y;
                if (w.x > max.x) max.x = w.x; if (w.y > max.y) max.y = w.y;
            }
        if (float.IsInfinity(min.x) || float.IsInfinity(min.y)) return;

        Vector2 center = (min + max) * 0.5f;
        origin = -(Vector3)center + new Vector3(cellSize * 0.5f, -(cellSize * 0.8660254f) * 0.5f, 0f);
    }

    public void Refill(System.Func<BlockType> randType, BoardManager board)
    {
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (!IsBlocked(x, y) && blocks[x, y] == null)
                    Spawn(randType(), x, y, board);
    }

    // ===== Neighbors / Radius =====
    public IEnumerable<Vector2Int> Neighbors(int x, int y)
    {
        var a = HexCoords.OffsetOddRToAxial(x, y);
        for (int dir = 0; dir < 6; dir++)
        {
            var n = HexCoords.Neighbor(a, dir);
            var off = HexCoords.AxialToOffsetOddR(n.q, n.r);
            yield return off;
        }
    }

    public List<Vector2Int> CellsInRadius(int x, int y, int r)
    {
        var result = new List<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var q = new Queue<(Vector2Int,int)>();
        var start = new Vector2Int(x, y);
        q.Enqueue((start, 0)); visited.Add(start);

        while (q.Count > 0)
        {
            var (p, d) = q.Dequeue();
            result.Add(p);
            if (d == r) continue;
            foreach (var nb in Neighbors(p.x, p.y))
            {
                if (!Inside(nb.x, nb.y)) continue;
                if (visited.Add(nb)) q.Enqueue((nb, d + 1));
            }
        }
        return result;
    }
}
