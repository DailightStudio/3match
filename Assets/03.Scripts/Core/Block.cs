using UnityEngine;
using System.Collections;

/// <summary>
/// 단일 블록(시각/이동/폭탄 상태 관리)
/// </summary>
public class Block : MonoBehaviour
{
    [HideInInspector] public int X;
    [HideInInspector] public int Y;
    public BlockType type;

    // Bomb(폭탄)
    public bool isBomb = false;
    public int bombRadius = 1;

    // 이동
    public bool IsMoving { get; private set; }
    Coroutine moveCo;
    const float arriveEps = 0.0001f;

    BoardManager board;
    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Init(BoardManager b, int x, int y, BlockType t)
    {
        board = b; 
        X = x; 
        Y = y; 
        type = t;

        if (!sr) sr = GetComponent<SpriteRenderer>();

        transform.position = board.WorldPos(x, y);
        transform.localScale = Vector3.one;
        isBomb = false;
        IsMoving = false;
        moveCo = null;
        name = $"Block_{type}_{x}_{y}";
    }

    public void SetGridPos(int x, int y) { X = x; Y = y; }

    public void MoveTo(int x, int y, float speed)
    {
        SetGridPos(x, y);
        var target = board.WorldPos(x, y);
        if (moveCo != null) board.StopCoroutine(moveCo);
        moveCo = board.StartCoroutine(MoveRoutine(target, Mathf.Max(0.0001f, speed)));
    }

    IEnumerator MoveRoutine(Vector3 target, float speed)
    {
        IsMoving = true;
        while ((transform.position - target).sqrMagnitude > arriveEps)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
        IsMoving = false;
        moveCo = null;
    }

    /// <summary>Bomb(폭탄) 외형 적용</summary>
    public void ApplyBombSprite(bool on, Sprite bombSprite)
    {
        isBomb = on;

        if (on) sr.sprite = bombSprite;
        transform.localScale = Vector3.one;
    }
}
