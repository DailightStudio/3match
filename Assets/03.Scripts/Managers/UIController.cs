using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("Refs")]
    public BoardManager board;

    [Header("Texts")]
    public Text scoreText;
    public Text movesText;
    public Text chainText;

    void Awake()
    {
        if (!board) board = FindObjectOfType<BoardManager>();
        if (!board) return;

        board.OnScoreChanged += HandleScore;
        board.OnMovesChanged += HandleMoves;
        board.OnChainBegin += HandleChainBegin;
        board.OnChainEnd += HandleChainEnd;

        // 초기 값 세팅
        HandleScore(board.Score);
        HandleMoves(board.startingMoves);
        HandleChainEnd();
    }

    void OnDestroy()
    {
        if (!board) return;
        // 이벤트 해제 (메모리/중복 방지)
        board.OnScoreChanged -= HandleScore;
        board.OnMovesChanged -= HandleMoves;
        board.OnChainBegin -= HandleChainBegin;
        board.OnChainEnd -= HandleChainEnd;
    }

    // ===== Handlers =====
    void HandleScore(int v)
    {
        if (scoreText) scoreText.text = v.ToString();
    }

    void HandleMoves(int v)
    {
        if (movesText) movesText.text = v.ToString();
    }

    void HandleChainBegin(int chain)
    {
        if (chainText)
        {
            chainText.gameObject.SetActive(true);
            chainText.text = $"x{chain}";
        }
    }

    void HandleChainEnd()
    {
        if (chainText)
        {
            chainText.text = "";
            chainText.gameObject.SetActive(false);
        }
    }
}
