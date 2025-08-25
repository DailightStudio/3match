using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 클리어/승격/점수/연출/정착 담당
/// </summary>
public class ResolveService
{
    readonly GridService grid;
    readonly GravityService gravity;

    public ResolveService(GridService grid)
    {
        this.grid = grid;
        this.gravity = new GravityService(grid);
    }

    public IEnumerator ClearAndScore(
        List<List<Vector2Int>> groups,
        List<Vector2Int> promote,
        int chain,
        System.Action<int> onScoreAdd,
        float clearAnimTime,
        int defaultBombRadius,
        Sprite[] bombSpritesByType,
        int scorePerBlock,
        int bombBonusPerBlock
    )
    {
        var promoteSet = new HashSet<Vector2Int>(promote);

        // 기본 제거: 승격 좌표는 항상 보호(동일 프레임 폭탄 연쇄에도 삭제되지 않게)
        var toClear = new HashSet<Block>();
        foreach (var g in groups)
        foreach (var c in g)
        {
            var bb = grid.Get(c.x, c.y);
            if (bb == null) continue;
            if (promoteSet.Contains(c)) continue; // ★ 승격 예정 칸 보호
            toClear.Add(bb);
        }

        // 폭탄 연쇄 확장 (승격 예정 칸은 제외하여 승격 보장)
        int bombExtra = 0;
        var snapshot = new List<Block>(toClear);
        foreach (var bb in snapshot)
        {
            if (bb != null && bb.isBomb)
            {
                foreach (var p in grid.CellsInRadius(bb.X, bb.Y, bb.bombRadius))
                {
                    if (promoteSet.Contains(p)) continue; // ★ 승격 보호
                    var nb = grid.Get(p.x, p.y);
                    if (nb != null && !toClear.Contains(nb))
                    {
                        toClear.Add(nb);
                        bombExtra++;
                    }
                }
            }
        }

        // 간단 연출
        float t = 0f;
        while (t < clearAnimTime)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1f, 0.1f, Mathf.Clamp01(t / clearAnimTime));
            foreach (var bb in toClear) if (bb) bb.transform.localScale = Vector3.one * s;
            yield return null;
        }

        // 삭제
        foreach (var bb in toClear)
        {
            if (!bb) continue;
            grid.Set(bb.X, bb.Y, null);
            Object.Destroy(bb.gameObject);
        }

        // 승격(폭탄)
        foreach (var c in promoteSet)
        {
            var bb = grid.Get(c.x, c.y);
            if (!bb) continue;
            bb.bombRadius = defaultBombRadius;

            int idx = (int)bb.type;
            if (bombSpritesByType == null || idx < 0 || idx >= bombSpritesByType.Length || bombSpritesByType[idx] == null)
                bb.ApplyBombSprite(true, null);
            else
                bb.ApplyBombSprite(true, bombSpritesByType[idx]);
        }

        // 점수
        int cleared = toClear.Count;
        float mult = (chain >= 4) ? 3f : (chain == 3 ? 2f : (chain == 2 ? 1.5f : 1f));
        int add = Mathf.RoundToInt((cleared * scorePerBlock + bombExtra * bombBonusPerBlock) * mult);
        if (add > 0) onScoreAdd?.Invoke(add);
    }

    public IEnumerator SettleFully(float fallSpeed, float settleTick)
    {
        int safety = grid.W * grid.H + 10;
        while (safety-- > 0)
        {
            bool moved = new GravityService(grid).Step(fallSpeed);
            yield return WaitForAllMovement();
            if (!moved) break;
            yield return new WaitForSeconds(settleTick);
        }
    }

    IEnumerator WaitForAllMovement()
    {
        bool moving;
        do
        {
            moving = false;
            for (int y = 0; y < grid.H; y++)
                for (int x = 0; x < grid.W; x++)
                {
                    var b = grid.Get(x, y);
                    if (b != null && b.IsMoving) { moving = true; break; }
                }
            if (moving) yield return null;
        } while (moving);
    }
}
