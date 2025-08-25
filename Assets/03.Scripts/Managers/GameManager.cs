using UnityEngine;

public class GameManager : MonoBehaviour
{
    public BoardManager board;

    public TextAsset levelJson;
    void Start()
    {
        BuildNow();
    }

    public void BuildNow()
    {
        if (!board)
        {
            Debug.LogError("boardManager 참조가 없습니다.");
            return;
        }
        if (!levelJson)
        {
            Debug.LogError("levelJson(TextAsset)이 비어있습니다.");
            return;
        }

        var level = LevelDefinition.FromJson(levelJson.text);
        board.Build(level);
    }
}
