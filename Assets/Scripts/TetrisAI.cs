using UnityEngine;
using System.Collections.Generic;

public class TetrisAI
{
    public struct Placement
    {
        public int RotationIndex;
        public Vector2Int Position;
        public float Score;
    }

    private readonly int boardWidth;
    private readonly int boardHeight;
    private int[,] boardData;

    public TetrisAI(int width, int height)
    {
        boardWidth = width;
        boardHeight = height;
    }

    /// <summary>
    /// Tìm placement tốt nhất cho tetromino hiện tại
    /// AI chỉ quyết định rotation và X position, piece sẽ rơi xuống tự động
    /// </summary>
    public Placement FindBestPlacement(int tetrominoIndex, int currentRotationIndex, Vector2Int currentPosition, int[,] currentBoardData)
    {
        // Copy board data để không ảnh hưởng đến game
        boardData = (int[,])currentBoardData.Clone();

        Placement bestPlacement = new()
        {
            RotationIndex = currentRotationIndex,
            Position = new Vector2Int(currentPosition.x, Board.Size.y - 1), // Đặt ở vị trí spawn (trên cùng)
            Score = float.MinValue
        };

        // Thử tất cả rotations
        for (int rotation = 0; rotation < 4; rotation++)
        {
            var tetromino = Tetrominoes.Get(tetrominoIndex, rotation);

            // Thử tất cả X positions (cột)
            for (int x = 0; x < boardWidth; x++)
            {
                // Vị trí spawn (piece sẽ rơi từ đây)
                var spawnPosition = new Vector2Int(x, Board.Size.y - 1);

                // Kiểm tra xem piece có thể spawn ở vị trí này không
                if (!IsValidPlacementForSpawn(tetromino, spawnPosition)) continue;

                // Tìm vị trí hạ cánh thực tế khi piece rơi xuống
                int landingY = FindLandingY(tetromino, x);
                var landingPosition = new Vector2Int(x, landingY);

                // Tính điểm cho placement này (dựa trên vị trí hạ cánh thực tế)
                float score = EvaluatePlacement(tetrominoIndex, tetromino, landingPosition);

                if (score > bestPlacement.Score)
                {
                    bestPlacement.Score = score;
                    bestPlacement.RotationIndex = rotation;
                    bestPlacement.Position = spawnPosition;
                }
            }
        }

        return bestPlacement;
    }

    /// <summary>
    /// Kiểm tra xem piece có thể spawn tại X position này không
    /// </summary>
    private bool IsValidPlacementForSpawn(Vector2Int[] tetromino, Vector2Int position)
    {
        foreach (var p in tetromino)
        {
            var point = position + p;

            // Kiểm tra boundary
            if (point.x < 0 || point.x >= boardWidth) return false;
            if (point.y < 0 || point.y >= boardHeight) return false;

            // Lúc spawn, không cần kiểm tra collision (piece chưa rơi xuống)
        }

        return true;
    }

    /// <summary>
    /// Tính vị trí Y nơi tetromino sẽ hạ cánh tại X position
    /// </summary>
    private int FindLandingY(Vector2Int[] tetromino, int x)
    {
        // Bắt đầu từ y=0 (đáy) và tìm vị trí cao nhất mà tetromino vẫn hợp lệ
        int landingY = 0;

        for (int y = 0; y < boardHeight; y++)
        {
            if (IsValidPlacement(tetromino, new Vector2Int(x, y)))
            {
                landingY = y;
            }
            else
            {
                // Không thể đặt ở đây, vậy vị trí hạ cánh là y - 1
                return landingY;
            }
        }

        // Nếu loop hết mà tetromino vẫn hợp lệ, trả về y cuối cùng
        return landingY;
    }

    /// <summary>
    /// Kiểm tra xem placement có hợp lệ không
    /// </summary>
    private bool IsValidPlacement(Vector2Int[] tetromino, Vector2Int position)
    {
        foreach (var p in tetromino)
        {
            var point = position + p;

            // Kiểm tra boundary
            if (point.x < 0 || point.x >= boardWidth) return false;
            if (point.y < 0 || point.y >= boardHeight) return false;

            // Kiểm tra collision với cells đã khóa
            if (boardData[point.y, point.x] > 0) return false;
        }

        return true;
    }

    /// <summary>
    /// Đánh giá chất lượng của một placement dựa trên vị trí hạ cánh thực tế
    /// </summary>
    private float EvaluatePlacement(int tetrominoIndex, Vector2Int[] tetromino, Vector2Int landingPosition)
    {
        // Tạo copy board để simulate
        int[,] simulatedBoard = (int[,])boardData.Clone();

        // Đặt tetromino tại vị trí hạ cánh thực tế
        foreach (var p in tetromino)
        {
            var point = landingPosition + p;
            if (point.y >= 0 && point.y < boardHeight && point.x >= 0 && point.x < boardWidth)
            {
                simulatedBoard[point.y, point.x] = 1;
            }
        }

        float score = 0f;

        // 1. Đánh giá số hàng được xóa (ưu tiên cao nhất)
        int clearedRowsCount = CountClearedRows(simulatedBoard, landingPosition, tetromino.Length);
        score += clearedRowsCount * 1000f; // Xóa 1 hàng: +1000 điểm

        // 2. Ưu tiên Tetris (xóa 4 hàng cùng lúc)
        if (clearedRowsCount == 4)
            score += 5000f; // Bonus cho Tetris

        // 3. Phạt cao độ của board (tránh stack quá cao)
        int boardHeightTemp = GetBoardHeight(simulatedBoard);
        score -= boardHeightTemp * 2f;

        // 4. Phạt số holes/gaps (tránh để lỗ trống)
        int holesCount = CountHoles(simulatedBoard);
        score -= holesCount * 50f;

        // 5. Ưu tiên độ bumpiness thấp (board phẳng hơn)
        float bumpiness = CalculateBumpiness(simulatedBoard);
        score -= bumpiness * 10f;

        // 6. Đặt tetromino ở giữa hơi ưu tiên (tăng tính tương tác)
        float positionPenalty = Mathf.Abs(landingPosition.x - boardWidth / 2f) * 1f;
        score -= positionPenalty;

        return score;
    }

    /// <summary>
    /// Đếm số hàng sẽ bị xóa dựa vào vị trí hạ cánh
    /// </summary>
    private int CountClearedRows(int[,] board, Vector2Int landingPosition, int tetrominoLength)
    {
        int clearedCount = 0;

        // Kiểm tra tất cả các hàng mà tetromino chiếm dụng
        int minY = Mathf.Max(0, landingPosition.y - 3);
        int maxY = Mathf.Min(boardHeight - 1, landingPosition.y);

        for (int r = minY; r <= maxY; r++)
        {
            bool isFullRow = true;
            for (int c = 0; c < boardWidth; c++)
            {
                if (board[r, c] == 0)
                {
                    isFullRow = false;
                    break;
                }
            }

            if (isFullRow)
                clearedCount++;
        }

        return clearedCount;
    }

    /// <summary>
    /// Tính chiều cao của board
    /// </summary>
    private int GetBoardHeight(int[,] board)
    {
        for (int r = boardHeight - 1; r >= 0; r--)
        {
            for (int c = 0; c < boardWidth; c++)
            {
                if (board[r, c] > 0)
                    return boardHeight - r;
            }
        }

        return 0;
    }

    /// <summary>
    /// Đếm số holes (cells trống bị che phủ bởi cells khác phía trên)
    /// </summary>
    private int CountHoles(int[,] board)
    {
        int holes = 0;

        for (int c = 0; c < boardWidth; c++)
        {
            bool blockFound = false;

            for (int r = boardHeight - 1; r >= 0; r--)
            {
                if (board[r, c] > 0)
                {
                    blockFound = true;
                }
                else if (blockFound && board[r, c] == 0)
                {
                    holes++;
                }
            }
        }

        return holes;
    }

    /// <summary>
    /// Tính độ lồi lõm của board (phẳng vs gồ ghề)
    /// </summary>
    private float CalculateBumpiness(int[,] board)
    {
        float bumpiness = 0f;
        int[] columnHeights = new int[boardWidth];

        // Tính chiều cao của mỗi column
        for (int c = 0; c < boardWidth; c++)
        {
            for (int r = boardHeight - 1; r >= 0; r--)
            {
                if (board[r, c] > 0)
                {
                    columnHeights[c] = boardHeight - r;
                    break;
                }
            }
        }

        // Tính tổng độ chênh lệch giữa các columns liền kề
        for (int c = 0; c < boardWidth - 1; c++)
        {
            bumpiness += Mathf.Abs(columnHeights[c] - columnHeights[c + 1]);
        }

        return bumpiness;
    }
}
