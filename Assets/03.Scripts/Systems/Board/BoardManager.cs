using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteAlways]
public class BoardManager : MonoBehaviour
{
    [Header("Board Size")]
    [HideInInspector] public int width;
    [HideInInspector] public int height;
    public float cellSize { get; set; } = 1f;

    [Header("Blocks")]
    public Block blockPrefab;
    public Sprite[] blockSprites;          // 일반 블록 스프라이트(이넘 타입순)

    [Header("Fall / Gravity")]
    public float fallSpeed = 8f;
    public bool simulateContinuously = true;

    [Header("Swap")]
    public float swapSpeed = 12f;

    [Header("Layout")]
    public Vector3 boardOrigin = Vector3.zero;
    public bool autoCenterOnBuild = true;

    [Header("Resolve")]
    public float clearAnimTime = 0.12f;
    public float settleTick = 0.02f;

    [Header("Special (Bomb/TMT)")]
    public int defaultBombRadius = 1;      // 인접 1칸씩 총 6칸
    public Sprite[] bombSpritesByType;     // 타입별 폭탄 스프라이트

    [Header("Scoring")]
    public int scorePerBlock = 10;
    public int bombBonusPerBlock = 5;
    public int startingMoves = 30;

    // Runtime
    public int Score { get; private set; }
    public int Moves { get; private set; }

    // 최근 스왑 좌표(승격 우선용)
    private Vector2Int lastMovedA = new Vector2Int(-1, -1);
    private Vector2Int lastMovedB = new Vector2Int(-1, -1);
    private Vector2Int currentSwapA = new Vector2Int(-1, -1);
    private Vector2Int currentSwapB = new Vector2Int(-1, -1);
    int currentChain = 0;

    // UI hooks
    public System.Action<int> OnScoreChanged;
    public System.Action<int> OnMovesChanged;
    public System.Action<int> OnChainBegin;
    public System.Action OnChainEnd;
    public System.Action OnNoMovesLeft;

    // Services
    GridService grid;
    GravityService gravity;
    MatchService matcher;
    ResolveService resolver;
    SwapService swapper;

    System.Random rng = new System.Random();

    void Awake()
    {
        grid = new GridService(transform);
        gravity = new GravityService(grid);
        matcher = new MatchService(grid);
        resolver = new ResolveService(grid);
        swapper = new SwapService(grid);

        if (Application.isPlaying) { Score = 0; Moves = startingMoves; }
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            if (simulateContinuously && grid != null && grid.IsReady)
                gravity.ContinuousTick(fallSpeed);
        }
        else
        {
            AutoCenterPreviewInEditor();
        }
    }

    void AutoCenterPreviewInEditor()
    {
        if (grid == null || !grid.IsReady) return;
        var before = boardOrigin;
        grid.CenterByCurrentBounds();
        boardOrigin = grid.Origin;
        if (before != boardOrigin) grid.RefreshAllWorldPositions();
    }

    [ContextMenu("Center Now")]
    public void CenterNow()
    {
        if (grid == null || !grid.IsReady) return;
        grid.CenterByCurrentBounds();
        boardOrigin = grid.Origin;
        grid.RefreshAllWorldPositions();
    }

    // ===== coords / build =====
    public Vector3 WorldPos(int x, int y)
    {
        if (grid != null && grid.IsReady) return grid.WorldPos(x, y);
        float xOffset = (y % 2 == 1) ? 0.5f * cellSize : 0f;
        float wx = (x * cellSize) + xOffset;
        float wy = -y * (cellSize * 0.8660254f);
        return transform.TransformPoint(boardOrigin + new Vector3(wx, wy, 0f));
    }

    public void Build(LevelDefinition level)
    {
        width = level.width; height = level.height;
        if (width <= 0 || height <= 0 || blockPrefab == null)
        {
            Debug.LogError("Build 실패: width/height 또는 blockPrefab 확인");
            return;
        }

        grid.Configure(width, height, cellSize, boardOrigin, blockPrefab, blockSprites);

        // 고정 배치 / 막힌 칸
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int v = level.Get(x, y);
                if (v < 0) { grid.SetBlocked(x, y, true); continue; }
                grid.Spawn((BlockType)v, x, y, this);
            }

        // 나머지 랜덤 채움 (막힌 칸 제외)
        grid.Refill(RandomType, this);

        if (autoCenterOnBuild)
        {
            grid.CenterByCurrentBounds();
            boardOrigin = grid.Origin;
            grid.RefreshAllWorldPositions();
        }

        Score = 0; Moves = startingMoves;
        OnScoreChanged?.Invoke(Score);
        OnMovesChanged?.Invoke(Moves);
        OnChainEnd?.Invoke();
    }

    BlockType RandomType()
    {
        // choose only enum values >= 0 (ignore None=-1)
        var values = (BlockType[])System.Enum.GetValues(typeof(BlockType));
        var valids = new System.Collections.Generic.List<BlockType>();
        foreach (var v in values) if ((int)v >= 0) valids.Add(v);
        if (valids.Count == 0) return 0;
        int idx = rng.Next(0, valids.Count);
        return valids[idx];
    }

    public bool Inside(int x, int y) => grid != null && grid.Inside(x, y);

    // ===== swap & validate =====
    public bool AreAdjacent(int x1, int y1, int x2, int y2)
        => swapper != null && swapper.AreAdjacent(x1, y1, x2, y2);

    public IEnumerator SwapAndValidate(int x1, int y1, int x2, int y2, object _unused = null)
    {
        if (!Inside(x1, y1) || !Inside(x2, y2)) yield break;
        if (!AreAdjacent(x1, y1, x2, y2)) yield break;
        if (Moves <= 0) { OnNoMovesLeft?.Invoke(); yield break; }

        lastMovedA = new Vector2Int(x1, y1);
        lastMovedB = new Vector2Int(x2, y2);

        bool prevSim = simulateContinuously; simulateContinuously = false;

        // 스왑
        yield return swapper.SwapOnce(x1, y1, x2, y2, swapSpeed);

        // 스왑 후 좌표/블록
        var aNow = new Vector2Int(x2, y2);
        var bNow = new Vector2Int(x1, y1);
        currentSwapA = aNow; currentSwapB = bNow;
        var aBlock = grid.Get(aNow.x, aNow.y);
        var bBlock = grid.Get(bNow.x, bNow.y);

        // 폭탄끼리 스왑 즉시 폭발
        if (aBlock && bBlock && aBlock.isBomb && bBlock.isBomb)
        {
            Moves = Mathf.Max(0, Moves - 1);
            OnMovesChanged?.Invoke(Moves);

            currentChain = 1; OnChainBegin?.Invoke(currentChain);

            var instant = new List<List<Vector2Int>>();
            instant.Add(new List<Vector2Int>(grid.CellsInRadius(aNow.x, aNow.y, aBlock.bombRadius)));
            instant.Add(new List<Vector2Int>(grid.CellsInRadius(bNow.x, bNow.y, bBlock.bombRadius)));

            yield return resolver.ClearAndScore(
                instant,
                new List<Vector2Int>(),
                currentChain,
                add => { Score += add; OnScoreChanged?.Invoke(Score); },
                clearAnimTime,
                defaultBombRadius,
                bombSpritesByType,
                scorePerBlock,
                bombBonusPerBlock
            );

            yield return resolver.SettleFully(fallSpeed, settleTick);
            grid.Refill(RandomType, this);
            yield return ResolveCascade();  // 이후 연쇄 전역
            OnChainEnd?.Invoke();

            simulateContinuously = prevSim;
            if (Moves <= 0) OnNoMovesLeft?.Invoke();
            yield break;
        }

        // 스왑으로 생긴 매치만 추림/ 전역에서 찾고 a/b 포함 그룹만 필터
        var allGroups = matcher.FindAll();
        var seeded = new List<List<Vector2Int>>();
        if (allGroups != null)
        {
            foreach (var g in allGroups)
            {
                if (g.Contains(aNow) || g.Contains(bNow)) seeded.Add(g);
            }
        }
        if (seeded.Count == 0)
        {
            // 매치가 없다면 롤백
            yield return swapper.SwapOnce(x2, y2, x1, y1, swapSpeed);
            simulateContinuously = prevSim;
            yield break;
        }

        Moves = Mathf.Max(0, Moves - 1);
        OnMovesChanged?.Invoke(Moves);

        // 첫 라운드만 처리(스왑으로 생긴 것만)
        yield return ResolveFromSeed(seeded);

        simulateContinuously = prevSim;
        if (Moves <= 0) OnNoMovesLeft?.Invoke();
    }

    // ===== Promotion selection =====

    Vector2Int? ChoosePromotionForGroup(List<Vector2Int> g)
    {
        if (g == null || g.Count < 4) return null;

        Vector2Int pick;

        if (g.Contains(currentSwapA)) { pick = currentSwapA; }
        else if (g.Contains(currentSwapB)) { pick = currentSwapB; }
        else { pick = g[g.Count / 2]; }

        return pick;
    }

    IEnumerator ResolveFromSeed(List<List<Vector2Int>> seeded)
    {
        if (seeded == null || seeded.Count == 0) yield break;

        currentChain = 1;
        OnChainBegin?.Invoke(currentChain);

        var promote = new List<Vector2Int>();
        foreach (var g in seeded)
        {
            if (g.Count >= 4)
            {
                var v = ChoosePromotionForGroup(g);
                if (v.HasValue) promote.Add(v.Value);
            }
        }

        yield return resolver.ClearAndScore(
            seeded,
            promote,
            currentChain,
            add => { Score += add; OnScoreChanged?.Invoke(Score); },
            clearAnimTime,
            defaultBombRadius,
            bombSpritesByType,
            scorePerBlock,
            bombBonusPerBlock
        );

        yield return resolver.SettleFully(fallSpeed, settleTick);
        grid.Refill(RandomType, this);

        yield return ResolveCascade();
        OnChainEnd?.Invoke();
    }

    // ===== Resolve (전역 연쇄) =====
    IEnumerator ResolveCascade()
    {
        currentChain = 0;

        while (true)
        {
            var groups = matcher.FindAll();
            if (groups == null || groups.Count == 0) break;

            currentChain++;
            OnChainBegin?.Invoke(currentChain);

            var promote = new List<Vector2Int>();
            foreach (var g in groups)
            {
                if (g.Count >= 4)
                {
                    Vector2Int pick = g[g.Count / 2];
                    var bb = grid.Get(pick.x, pick.y);
                    if (bb != null && bb.isBomb)
                    {
                        bool replaced = false;
                        foreach (var c in g)
                        {
                            var bc = grid.Get(c.x, c.y);
                            if (bc != null && !bc.isBomb) { pick = c; replaced = true; break; }
                        }
                        if (!replaced) continue;
                    }
                    promote.Add(pick);
                }
            }

            yield return resolver.ClearAndScore(
                groups,
                promote,
                currentChain,
                add => { Score += add; OnScoreChanged?.Invoke(Score); },
                clearAnimTime,
                defaultBombRadius,
                bombSpritesByType,
                scorePerBlock,
                bombBonusPerBlock
            );

            yield return resolver.SettleFully(fallSpeed, settleTick);
            grid.Refill(RandomType, this);
            yield return new WaitForSeconds(0.02f);
        }

        OnChainEnd?.Invoke();
    }

    // 카메라 핏에서 사용
    public bool GetBoardWorldBounds(out Vector3 min, out Vector3 max)
    {
        min = max = Vector3.zero;
        if (grid == null || !grid.IsReady) return false;
        grid.GetWorldBounds(out min, out max);
        return !(min == max);
    }
}
