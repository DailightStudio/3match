using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CameraAutoFit : MonoBehaviour
{
    public BoardManager board;
    public float margin = 0.6f;         // 보드 가장자리 여유
    public bool fitOnceOnStart { get; set; } = true;

    Camera cam;
    bool fitted = false;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (!cam.orthographic) cam.orthographic = true;
    }

    void Start()
    {
        if (fitOnceOnStart) StartCoroutine(FitOnceWhenReady());
    }

    public IEnumerator FitOnceWhenReady()
    {
        // 보드/경계가 준비될 때까지 대기
        if (!board) board = FindObjectOfType<BoardManager>();
        if (!board) yield break;

        Vector3 min, max;
        // 경계가 유효해질 때까지 다음 프레임에서 재확인
        while (!board.GetBoardWorldBounds(out min, out max))
            yield return null;

        FitNow(min, max);
        fitted = true;
    }

    public void FitNow(Vector3 min, Vector3 max)
    {
        // 폭/높이 0 보호
        if (min == max) return;

        // 보드 중심과 크기
        Vector3 center = (min + max) * 0.5f;
        Vector3 size = (max - min);

        // 마진 적용
        size.x += margin * 2f;
        size.y += margin * 2f;

        // 카메라 위치(보드 중앙)
        transform.position = new Vector3(center.x, center.y, transform.position.z);

        // 직교 카메라 사이즈 계산(가로/세로 중 더 큰 쪽 기준)
        float halfHeight = size.y * 0.5f;
        float halfWidth = size.x * 0.5f / cam.aspect;

        cam.orthographicSize = Mathf.Max(halfHeight, halfWidth);
    }
}
