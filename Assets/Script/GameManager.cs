using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager Instance;

    public int width = 5;
    public int height = 8;
    public Panel panelPrefab;
    public Sprite[] numberSprites; // 1〜9対応

    private Panel[,] board;

    int actionCount = 0;
    System.Random rand = new System.Random();

    void Awake(){
        Instance = this;
    }

    void Start(){
        InitBoard();
    }

    void InitBoard(){
        board = new Panel[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CreatePanel(x, y, Random.Range(1, 10));
            }
        }
    }

    void CreatePanel(int x, int y, int num){
        Panel newPanel = Instantiate(panelPrefab, new Vector3(x, y, 0), Quaternion.identity);
        newPanel.SetNumber(num, numberSprites[num - 1]);
        newPanel.boardPos = new Vector2Int(x, y);
        board[x, y] = newPanel;
    }

    public void OnPanelClicked(Panel panel)
    {
        TryRemove(panel.boardPos);

        actionCount++;
        if (actionCount % 3 == 0)
        {
            PushUpNewRow();
        }
    }

    // 隣接探索
    List<Vector2Int> GetConnectedSameNumbers(Vector2Int startPos)
    {
        Panel startPanel = board[startPos.x, startPos.y];
        if (startPanel == null) return new List<Vector2Int>();

        int targetNum = startPanel.number;
        var connected = new List<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();

        queue.Enqueue(startPos);
        visited.Add(startPos);

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (queue.Count > 0)
        {
            var pos = queue.Dequeue();
            connected.Add(pos);

            for (int i = 0; i < 4; i++)
            {
                int nx = pos.x + dx[i];
                int ny = pos.y + dy[i];

                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                var nextPos = new Vector2Int(nx, ny);
                if (!visited.Contains(nextPos) && board[nx, ny] != null && board[nx, ny].number == targetNum)
                {
                    visited.Add(nextPos);
                    queue.Enqueue(nextPos);
                }
            }
        }
        return connected;
    }

    // 消去
    void TryRemove(Vector2Int pos)
    {
        var connected = GetConnectedSameNumbers(pos);
        if (connected.Count >= board[pos.x, pos.y].number)
        {
            foreach (var c in connected)
            {
                Destroy(board[c.x, c.y].gameObject);
                board[c.x, c.y] = null;
            }
            ApplyGravity();
        }
    }
    void ApplyGravity()
    {
        for (int x = 0; x < width; x++)
        {
            int writeY = 0;
            for (int y = 0; y < height; y++)
            {
                if (board[x, y] != 0)
                {
                    board[x, writeY] = board[x, y];
                    if (writeY != y) board[x, y] = 0;
                    writeY++;
                }
            }
        }
    }
    void OnClick(Vector2Int pos)
    {
        TryRemove(pos);

        actionCount++;
        if (actionCount % 3 == 0)
        {
            PushUpNewRow();
        }
    }

    void PushUpNewRow()
    {
        // 上に詰める（最上段が溢れたらゲームオーバーなど）
        for (int y = height - 1; y > 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                board[x, y] = board[x, y - 1];
            }
        }
        // 新しい行を生成（ランダム1〜9）
        for (int x = 0; x < width; x++)
        {
            board[x, 0] = rand.Next(1, 10);
        }
    }

}
