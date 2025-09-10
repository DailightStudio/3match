public class GravityService
{
    readonly GridService grid;
    public GravityService(GridService grid) { this.grid = grid; }

    public void ContinuousTick(float fallSpeed)
    {
        if (!grid.IsReady) return;
        for (int y = grid.H - 2; y >= 0; y--)
            for (int x = 0; x < grid.W; x++)
            {
                var b = grid.Get(x, y);
                if (!b) continue;

                bool isOdd = (y % 2) == 1;
                int dlx = isOdd ? x : x - 1;
                int drx = isOdd ? x + 1 : x;
                int ny = y + 1;

                if (grid.Inside(dlx, ny) && grid.Get(dlx, ny) == null)
                { grid.MoveBlock(b, dlx, ny, fallSpeed); continue; }
                if (grid.Inside(drx, ny) && grid.Get(drx, ny) == null)
                { grid.MoveBlock(b, drx, ny, fallSpeed); continue; }
            }
    }
    public bool Step(float fallSpeed)
    {
        bool moved = false;

        // 위에서 아래로 스캔: y = H-2 ~ 0
        for (int y = grid.H - 2; y >= 0; y--)
        {
            for (int x = 0; x < grid.W; x++)
            {
                var b = grid.Get(x, y);
                if (!b) continue; // 비었으면 스킵

                // 대각(odd-r 오프셋)
                bool isOdd = (y & 1) == 1;
                int dlx = isOdd ? x : x - 1; // 좌하
                int drx = isOdd ? x + 1 : x;     // 우하
                int ny = y + 1;

                if (grid.Inside(dlx, ny) && grid.Get(dlx, ny) == null)
                {
                    grid.MoveBlock(b, dlx, ny, fallSpeed);
                    moved = true;
                    continue;
                }
                if (grid.Inside(drx, ny) && grid.Get(drx, ny) == null)
                {
                    grid.MoveBlock(b, drx, ny, fallSpeed);
                    moved = true;
                    continue;
                }
            }
        }

        return moved;
    }

}
