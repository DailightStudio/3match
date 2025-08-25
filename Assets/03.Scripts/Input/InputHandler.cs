using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public BoardManager board;
    Vector2Int? first;

    void Awake()
    {
        if (!board) board = GetComponent<BoardManager>();
    }

    void Update()
    {
        if (!board) return;

        // 화면 밖 클릭 무시(에디터 경고 방지)
        var mp = Input.mousePosition;
        if (mp.x < 0 || mp.y < 0 || mp.x > Screen.width || mp.y > Screen.height) return;

        if (Input.GetMouseButtonDown(0))
        {
            first = ScreenToCell(mp);
        }
        else if (Input.GetMouseButtonUp(0) && first.HasValue)
        {
            var second = ScreenToCell(mp);
            var a = first.Value;
            var b = second;

            if (board.Inside(a.x, a.y) && board.Inside(b.x, b.y) && board.AreAdjacent(a.x, a.y, b.x, b.y))
                StartCoroutine(board.SwapAndValidate(a.x, a.y, b.x, b.y));

            first = null;
        }
    }

    Vector2Int ScreenToCell(Vector3 screen)
    {
        Vector3 wp = Camera.main.ScreenToWorldPoint(screen);
        // odd‑r 역변환: BoardManager.WorldPos와 동일한 수식의 역
        // 근사치라 셀 중앙 스냅: x = round((wx - xOffset)/cellSize), y = round(-wy/HexV)
        float hexV = board.cellSize * 0.8660254f;
        Vector3 local = Quaternion.Inverse(board.transform.rotation) * (wp - board.transform.TransformPoint(board.boardOrigin));
        float y = -local.y / hexV;
        int iy = Mathf.RoundToInt(y);

        float xOffset = (iy % 2 == 1) ? 0.5f * board.cellSize : 0f;
        float x = (local.x - xOffset) / board.cellSize;
        int ix = Mathf.RoundToInt(x);

        return new Vector2Int(ix, iy);
    }
}
