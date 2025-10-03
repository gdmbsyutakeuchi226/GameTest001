using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // 新InputSystem用
public class BoardManager : MonoBehaviour {
    [Header("Board Size")]
    public int width = 5;
    public int height = 8;

    [Header("Tile Prefabs (Block_1 ~ Block_9)")]
    public GameObject[] numberPrefabs; // index 0 -> number 1

    [Header("Visual")]
    public float spacing = 1.2f;       // world units
    public float fallDuration = 0.12f; // seconds
    public float destroyDuration = 0.18f; // タイル消滅アニメ時間（NumberTile と合わせる）

    [Header("Rules")]
    public int maxNumber = 9;
    public int minMatch = 3; // 3つ以上でマッチ
    public bool allowDiagonalMatches = false; // 斜め方向のマッチを許可するか
    
    [Header("Game Mode")]
    public bool isFallingPuzzleMode = true; // 落ちものパズルモード
    public int initialMaxNumber = 3; // 初期生成する最大数字
    public int initialRows = 4; // 初期生成する行数
    
    [Header("Falling Blocks")]
    public float fallingBlockInterval = 3.0f; // 新しいブロックが落ちる間隔（秒）
    public int fallingBlockCount = 2; // 一度に落ちるブロック数（ターン制では2つ）
    public int fallingBlockMaxNumber = 3; // 落ちるブロックの最大数字
    
    [Header("Game Over")]
    public int gameOverRow = 6; // この行までブロックが積もったらゲームオーバー

    [Header("UI")]
    public Text scoreText;

    [Header("Input")]
    public LayerMask tileLayerMask; // Inspector で Tile 用のレイヤーを指定すると安定

    private NumberTile[,] grid;
    private bool isBusy = false;
    private int score = 0;
    private Coroutine fallingBlockCoroutine;

    void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        ClearBoard();
        InitBoard();
        UpdateScoreText();
        AdjustCameraSize();
        
        if (isFallingPuzzleMode)
        {
            // ターン制モードでは自動落下を無効化
            // StartFallingBlocks();
        }
    }
    
    void StartFallingBlocks()
    {
        if (fallingBlockCoroutine != null)
        {
            StopCoroutine(fallingBlockCoroutine);
        }
        fallingBlockCoroutine = StartCoroutine(FallingBlocksCoroutine());
    }
    
    void StopFallingBlocks()
    {
        if (fallingBlockCoroutine != null)
        {
            StopCoroutine(fallingBlockCoroutine);
            fallingBlockCoroutine = null;
        }
    }

    void ClearBoard()
    {
        if (grid != null)
        {
            foreach (var t in grid)
                if (t != null) Destroy(t.gameObject);
        }
        grid = new NumberTile[width, height];
        score = 0;
    }

    void InitBoard()
    {
        if (isFallingPuzzleMode)
        {
            InitFallingPuzzleBoard();
        }
        else
        {
            InitNormalBoard();
        }
    }
    
    void InitFallingPuzzleBoard()
    {
        // 落ちものパズルモード：下から4段だけ1-3の数字で生成
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < initialRows; y++)
            {
                int number = Random.Range(1, initialMaxNumber + 1);
                CreateTileAt(x, y, number, animate: false);
            }
        }
        
        // 上部は空のまま（後でブロックが落ちてくる）
        Debug.Log($"Initialized falling puzzle board with {initialRows} rows of numbers 1-{initialMaxNumber}");
    }
    
    void InitNormalBoard()
    {
        // 通常モード：全体をランダム生成
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int number = RandomNumber();
                CreateTileAt(x, y, number, animate: false);
            }
        }
        
        // マッチする組み合わせが存在するかチェック
        if (!HasValidMoves())
        {
            Debug.Log("No valid moves found, regenerating board...");
            ClearBoard();
            InitBoard();
        }
    }
    
    bool HasValidMoves()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null)
                {
                    List<Vector2Int> connected = FindConnected(x, y, grid[x, y].value);
                    if (connected.Count >= minMatch)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    int RandomNumber() => Random.Range(1, maxNumber + 1);

    Vector3 GetCellPosition(int x, int y)
    {
        float originX = -(width - 1) * spacing * 0.5f;
        float originY = -(height - 1) * spacing * 0.5f;
        return new Vector3(originX + x * spacing, originY + y * spacing, 0f);
    }

    NumberTile CreateTileAt(int x, int y, int number, bool animate)
    {
        Vector3 finalPos = GetCellPosition(x, y);
        Vector3 spawnPos = finalPos;
        if (animate) spawnPos += Vector3.up * (height * 0.5f + 1f);

        var prefab = numberPrefabs[number - 1];
        var go = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
        var tile = go.GetComponent<NumberTile>();
        tile.Initialize(this, x, y, number);
        grid[x, y] = tile;

        if (animate)
            tile.MoveTo(finalPos, fallDuration);
        else
            tile.transform.position = finalPos;

        return tile;
    }

    void Update(){
        if (isBusy) return;

        // マウスクリック
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            TryHandlePointer(screenPos);
        }

        // タッチ
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            TryHandlePointer(screenPos);
        }
    }

    void TryHandlePointer(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 wp = cam.ScreenToWorldPoint(screenPos);
        Vector2 p2 = new Vector2(wp.x, wp.y);
        Collider2D hit = Physics2D.OverlapPoint(p2, tileLayerMask);
        if (hit != null)
        {
            var tile = hit.GetComponent<NumberTile>();
            if (tile != null) OnTileClicked(tile.x, tile.y);
        }
    }

    public void OnTileClicked(int x, int y)
    {
        if (isBusy) return;
        if (!InBounds(x, y) || grid[x, y] == null) return;

        int target = grid[x, y].value;
        List<Vector2Int> matched = FindConnected(x, y, target);

        Debug.Log($"Clicked tile at ({x}, {y}) with value {target}. Found {matched.Count} connected tiles.");

        if (matched.Count < minMatch) 
        {
            Debug.Log($"Not enough matches: {matched.Count} < {minMatch}");
            // ハイライトを一時的に表示
            StartCoroutine(ShowHighlight(matched));
            return;
        }

        // マッチしたタイルをハイライト
        foreach (var pos in matched)
        {
            if (grid[pos.x, pos.y] != null)
            {
                grid[pos.x, pos.y].Highlight(true);
            }
        }

        if (isFallingPuzzleMode)
        {
            StartCoroutine(MergeAndCollapse(matched));
        }
        else
        {
            StartCoroutine(RemoveAndCollapse(matched));
        }
    }
    
    IEnumerator ShowHighlight(List<Vector2Int> positions)
    {
        // マッチしたタイルをハイライト
        foreach (var pos in positions)
        {
            if (grid[pos.x, pos.y] != null)
            {
                grid[pos.x, pos.y].Highlight(true);
            }
        }
        
        yield return new WaitForSeconds(0.3f);
        
        // ハイライトを解除
        foreach (var pos in positions)
        {
            if (grid[pos.x, pos.y] != null)
            {
                grid[pos.x, pos.y].Highlight(false);
            }
        }
    }

    bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

    List<Vector2Int> FindConnected(int startX, int startY, int targetValue)
    {
        bool[,] visited = new bool[width, height];
        List<Vector2Int> result = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        
        // 開始位置をキューに追加
        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        // 移動ベクトル（斜め方向を含む）
        Vector2Int[] directions;
        if (allowDiagonalMatches)
        {
            directions = new Vector2Int[] { 
                Vector2Int.up, 
                Vector2Int.down, 
                Vector2Int.left, 
                Vector2Int.right,
                new Vector2Int(1, 1),   // 右上
                new Vector2Int(1, -1),  // 右下
                new Vector2Int(-1, 1),  // 左上
                new Vector2Int(-1, -1)  // 左下
            };
        }
        else
        {
            directions = new Vector2Int[] { 
                Vector2Int.up, 
                Vector2Int.down, 
                Vector2Int.left, 
                Vector2Int.right 
            };
        }

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            result.Add(current);

            // 4方向をチェック
            foreach (Vector2Int direction in directions)
            {
                int newX = current.x + direction.x;
                int newY = current.y + direction.y;

                // 境界チェック
                if (!InBounds(newX, newY)) continue;
                
                // 既に訪問済みかチェック
                if (visited[newX, newY]) continue;
                
                // タイルが存在し、同じ値かチェック
                if (grid[newX, newY] != null && grid[newX, newY].value == targetValue)
                {
                    visited[newX, newY] = true;
                    queue.Enqueue(new Vector2Int(newX, newY));
                }
            }
        }
        
        return result;
    }

    IEnumerator MergeAndCollapse(List<Vector2Int> matched)
    {
        isBusy = true;

        // ハイライトを解除
        foreach (var v in matched)
        {
            if (grid[v.x, v.y] != null)
            {
                grid[v.x, v.y].Highlight(false);
            }
        }

        // マッチしたタイルの値を取得（最初のタイルの値）
        int originalValue = grid[matched[0].x, matched[0].y].value;
        int newValue = originalValue + 1; // 数値を1加算
        
        Debug.Log($"Merging {matched.Count} tiles of value {originalValue} into value {newValue}");

        // 最初のタイルの位置を保持
        Vector2Int mergePosition = matched[0];
        
        // 他のタイルを削除
        for (int i = 1; i < matched.Count; i++)
        {
            var v = matched[i];
            var tile = grid[v.x, v.y];
            if (tile != null)
            {
                grid[v.x, v.y] = null;
                StartCoroutine(tile.PlayDestroyAnimationCoroutine(destroyDuration));
            }
        }

        // 最初のタイルの値を更新
        if (grid[mergePosition.x, mergePosition.y] != null)
        {
            grid[mergePosition.x, mergePosition.y].value = newValue;
            grid[mergePosition.x, mergePosition.y].UpdateVisual();
        }

        // スコア
        score += matched.Count * 10;
        UpdateScoreText();

        // 一定時間待ってから落下処理
        yield return new WaitForSeconds(destroyDuration + 0.02f);

        // ターン制：重力は適用せず、手動で次のターンを開始
        isBusy = false;
        
        // ターン終了後に新しいパネルを落とす
        yield return StartCoroutine(EndTurnAndSpawnPanels());
    }

    IEnumerator RemoveAndCollapse(List<Vector2Int> matched)
    {
        isBusy = true;

        // ハイライトを解除
        foreach (var v in matched)
        {
            if (grid[v.x, v.y] != null)
            {
                grid[v.x, v.y].Highlight(false);
            }
        }

        // 消滅アニメ開始（非同期）
        foreach (var v in matched)
        {
            var tile = grid[v.x, v.y];
            if (tile != null)
            {
                grid[v.x, v.y] = null;
                // StartCoroutine を呼び出して BoardManager 側で直接待つ（ここでは固定待ち）
                StartCoroutine(tile.PlayDestroyAnimationCoroutine(destroyDuration));
            }
        }

        // スコア
        score += matched.Count * 10;
        UpdateScoreText();

        // 一定時間待ってから落下処理（destroyDuration に合わせる）
        yield return new WaitForSeconds(destroyDuration + 0.02f);

        // 各列を下に詰める
        for (int x = 0; x < width; x++)
        {
            List<NumberTile> kept = new List<NumberTile>();
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null) kept.Add(grid[x, y]);
                grid[x, y] = null;
            }

            int writeY = 0;
            for (int i = 0; i < kept.Count; i++)
            {
                NumberTile t = kept[i];
                grid[x, writeY] = t;
                t.SetCoordinates(x, writeY);
                t.MoveTo(GetCellPosition(x, writeY), fallDuration);
                writeY++;
            }

            for (int y = writeY; y < height; y++)
            {
                CreateTileAt(x, y, RandomNumber(), animate: true);
            }
        }

        // 落下アニメが終わるまで待つ
        yield return new WaitForSeconds(fallDuration + 0.02f);
        
        // ターン制：重力は適用せず、手動で次のターンを開始
        isBusy = false;
        
        // ターン終了後に新しいパネルを落とす
        yield return StartCoroutine(EndTurnAndSpawnPanels());
    }

    IEnumerator CheckForCascades()
    {
        bool foundCascade = true;
        int cascadeCount = 0;
        
        while (foundCascade)
        {
            foundCascade = false;
            cascadeCount++;
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != null)
                    {
                        List<Vector2Int> connected = FindConnected(x, y, grid[x, y].value);
                        if (connected.Count >= minMatch)
                        {
                            Debug.Log($"Cascade {cascadeCount}: Found {connected.Count} connected tiles");
                            
                            // 連鎖ボーナススコア
                            score += connected.Count * 10 * cascadeCount;
                            UpdateScoreText();
                            
                            // マッチしたタイルをハイライト
                            foreach (var pos in connected)
                            {
                                if (grid[pos.x, pos.y] != null)
                                {
                                    grid[pos.x, pos.y].Highlight(true);
                                }
                            }
                            
                            yield return new WaitForSeconds(0.2f);
                            
                            // ハイライトを解除して削除
                            foreach (var pos in connected)
                            {
                                if (grid[pos.x, pos.y] != null)
                                {
                                    grid[pos.x, pos.y].Highlight(false);
                                }
                            }
                            
                            // 直接削除処理を行う（RemoveAndCollapseを呼ばない）
                            yield return StartCoroutine(RemoveTilesOnly(connected));
                            foundCascade = true;
                            break;
                        }
                    }
                }
                if (foundCascade) break;
            }
        }
    }
    
    IEnumerator RemoveTilesOnly(List<Vector2Int> matched)
    {
        // 消滅アニメ開始（非同期）
        foreach (var v in matched)
        {
            var tile = grid[v.x, v.y];
            if (tile != null)
            {
                grid[v.x, v.y] = null;
                StartCoroutine(tile.PlayDestroyAnimationCoroutine(destroyDuration));
            }
        }

        // 一定時間待ってから落下処理
        yield return new WaitForSeconds(destroyDuration + 0.02f);

        // 各列を下に詰める
        for (int x = 0; x < width; x++)
        {
            List<NumberTile> kept = new List<NumberTile>();
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null) kept.Add(grid[x, y]);
                grid[x, y] = null;
            }

            int writeY = 0;
            for (int i = 0; i < kept.Count; i++)
            {
                NumberTile t = kept[i];
                grid[x, writeY] = t;
                t.SetCoordinates(x, writeY);
                t.MoveTo(GetCellPosition(x, writeY), fallDuration);
                writeY++;
            }

            for (int y = writeY; y < height; y++)
            {
                CreateTileAt(x, y, RandomNumber(), animate: true);
            }
        }

        // 落下アニメが終わるまで待つ
        yield return new WaitForSeconds(fallDuration + 0.02f);
    }

    IEnumerator ApplyGravity()
    {
        bool moved = true;
        int gravitySteps = 0;
        
        while (moved && gravitySteps < height) // 無限ループ防止
        {
            moved = false;
            gravitySteps++;
            
            // 下から上に向かってチェック
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grid[x, y] == null && grid[x, y + 1] != null)
                    {
                        // 上のタイルを下に移動
                        grid[x, y] = grid[x, y + 1];
                        grid[x, y + 1] = null;
                        grid[x, y].SetCoordinates(x, y);
                        grid[x, y].MoveTo(GetCellPosition(x, y), fallDuration);
                        moved = true;
                    }
                }
            }
            
            if (moved)
            {
                yield return new WaitForSeconds(fallDuration + 0.02f);
            }
        }
        
        Debug.Log($"Gravity applied in {gravitySteps} steps");
    }

    IEnumerator FallingBlocksCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(fallingBlockInterval);
            
            if (!isBusy)
            {
                yield return StartCoroutine(SpawnFallingBlocks());
            }
        }
    }
    
    IEnumerator SpawnFallingBlocks()
    {
        isBusy = true;
        
        // ランダムな位置に新しいブロックを生成
        List<int> availableColumns = new List<int>();
        for (int x = 0; x < width; x++)
        {
            if (grid[x, height - 1] == null) // 最上段が空
            {
                availableColumns.Add(x);
            }
        }
        
        if (availableColumns.Count > 0)
        {
            int blocksToSpawn = Mathf.Min(fallingBlockCount, availableColumns.Count);
            
            for (int i = 0; i < blocksToSpawn; i++)
            {
                int randomColumn = availableColumns[Random.Range(0, availableColumns.Count)];
                availableColumns.Remove(randomColumn); // 重複を避ける
                
                int randomNumber = Random.Range(1, fallingBlockMaxNumber + 1);
                CreateTileAt(randomColumn, height - 1, randomNumber, animate: true);
            }
            
            Debug.Log($"Spawned {blocksToSpawn} falling blocks");
        }
        
        // 重力を適用
        yield return StartCoroutine(ApplyGravity());
        
        // ゲームオーバーチェック
        if (CheckGameOver())
        {
            Debug.Log("Game Over! Blocks reached the top.");
            StopFallingBlocks();
            
        }
        
        // 連鎖チェック
        yield return StartCoroutine(CheckForCascades());
        
        isBusy = false;
    }

    bool CheckGameOver()
    {
        // 指定された行にブロックがあるかチェック
        for (int x = 0; x < width; x++)
        {
            if (grid[x, gameOverRow] != null)
            {
                return true;
            }
        }
        return false;
    }

    IEnumerator EndTurnAndSpawnPanels()
    {
        // ターン終了後の処理
        Debug.Log("Turn ended. Spawning new panels...");
        
        // 少し待ってから新しいパネルを生成
        yield return new WaitForSeconds(0.5f);
        
        // 2つのパネルを1組で落とす
        yield return StartCoroutine(SpawnTurnPanels());
    }
    
    IEnumerator SpawnTurnPanels()
    {
        isBusy = true;
        
        // ランダムな位置に2つのパネルを生成
        List<int> availableColumns = new List<int>();
        for (int x = 0; x < width; x++)
        {
            if (grid[x, height - 1] == null) // 最上段が空
            {
                availableColumns.Add(x);
            }
        }
        
        if (availableColumns.Count >= 2)
        {
            // 2つの異なる列を選択
            int column1 = availableColumns[Random.Range(0, availableColumns.Count)];
            availableColumns.Remove(column1);
            int column2 = availableColumns[Random.Range(0, availableColumns.Count)];
            
            // 2つのパネルを生成
            int number1 = Random.Range(1, fallingBlockMaxNumber + 1);
            int number2 = Random.Range(1, fallingBlockMaxNumber + 1);
            
            CreateTileAt(column1, height - 1, number1, animate: true);
            CreateTileAt(column2, height - 1, number2, animate: true);
            
            Debug.Log($"Spawned 2 panels: {number1} at column {column1}, {number2} at column {column2}");
        }
        else if (availableColumns.Count == 1)
        {
            // 1つの列に2つのパネルを重ねて生成（特殊ケース）
            int column = availableColumns[0];
            int number1 = Random.Range(1, fallingBlockMaxNumber + 1);
            int number2 = Random.Range(1, fallingBlockMaxNumber + 1);
            
            CreateTileAt(column, height - 1, number1, animate: true);
            // 2つ目は1つ下に生成
            if (height > 1 && grid[column, height - 2] == null)
            {
                CreateTileAt(column, height - 2, number2, animate: true);
            }
            
            Debug.Log($"Spawned 2 panels in same column: {number1} and {number2}");
        }
        
        // 重力を適用
        yield return StartCoroutine(ApplyGravity());
        
        // ゲームオーバーチェック
        if (CheckGameOver())
        {
            Debug.Log("Game Over! Blocks reached the top.");
            yield return null;
        }
        
        // 連鎖チェック
        yield return StartCoroutine(CheckForCascades());
        
        isBusy = false;
    }

    void UpdateScoreText()
    {
        if (scoreText != null) scoreText.text = $"Score: {score}";
    }

    void AdjustCameraSize()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float aspect = (float)Screen.width / Screen.height;
        float targetHeight = (height - 1) * spacing + spacing; // 全体高さ
        float targetWidth = (width - 1) * spacing + spacing;

        float sizeByHeight = targetHeight * 0.5f * 1.05f; // 余白
        float sizeByWidth = (targetWidth * 0.5f * 1.05f) / aspect;

        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }
}