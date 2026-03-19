using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    [SerializeField] private Board board;

    // Trọng số Heuristics (đã được tuning chuẩn cho Tetris)
    private const float WeightHeight = -0.510066f;
    private const float WeightLines = 0.760666f;
    private const float WeightHoles = -0.35663f;
    private const float WeightBumpiness = -0.184483f;

    // Struct đại diện cho state của bảng, dùng value type để không tạo rác (GC)
    private struct BoardState
    {
        public uint[] Rows; // 20 hàng, mỗi hàng dùng 10 bit (từ 0-9)
        public int Width;
        public int Height;

        public BoardState(int width, int height)
        {
            Width = width;
            Height = height;
            Rows = new uint[height];
        }

        // Deep copy nhanh
        public BoardState Clone()
        {
            BoardState copy = new BoardState(Width, Height);
            System.Array.Copy(Rows, copy.Rows, Height);
            return copy;
        }

        public void SetBit(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                Rows[y] |= (1u << x);
        }

        public bool GetBit(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return true; // Ra ngoài coi như đụng tường
            return (Rows[y] & (1u << x)) != 0;
        }
    }

    private struct Move
    {
        public Vector2Int Position;
        public int Rotation;
        public float Score;
        public BoardState State;
    }

    public void GetBestMove(out Vector2Int bestPosition, out int bestRotation)
    {
        bestPosition = Vector2Int.zero;
        bestRotation = 0;

        if (board == null) return;

        // 1. Lấy trạng thái bảng hiện tại chuyển sang Bitboard
        // BoardState initialState = GetCurrentBoardState();
        // int currentPiece = board.TetrominoIndex;

        BoardState initialState = GetPredictBoardState();
        int currentPiece = Random.Range(0, Tetrominoes.Length);

        Debug.Log(currentPiece);

        // Giả sử Board của bạn có NextPiece. Nếu không có, cứ truyền -1 để nó chạy 1-Ply
        int nextPiece = -1; // Thay bằng board.NextTetrominoIndex nếu có

        // 2. Lấy tất cả nước đi của mảnh hiện tại (Ply 1)
        List<Move> ply1Moves = GetAllValidMoves(initialState, currentPiece);

        if (ply1Moves.Count == 0) return;

        // Nếu không có mảnh tiếp theo, chỉ cần trả về nước đi tốt nhất của Ply 1
        if (nextPiece == -1)
        {
            Move best = ply1Moves[0];
            foreach (var m in ply1Moves) if (m.Score > best.Score) best = m;
            bestPosition = best.Position;
            bestRotation = best.Rotation;
            return;
        }

        // 3. BEAM SEARCH cho Ply 2 (Look-ahead)
        // Lọc top K nước đi tốt nhất để tránh bùng nổ tổ hợp
        int beamWidth = 6;
        ply1Moves.Sort((a, b) => b.Score.CompareTo(a.Score));
        int limit = Mathf.Min(beamWidth, ply1Moves.Count);

        float globalBestScore = float.MinValue;

        for (int i = 0; i < limit; i++)
        {
            Move move1 = ply1Moves[i];

            // Tìm nước đi cho mảnh Next trên bảng đã cập nhật của Move 1
            List<Move> ply2Moves = GetAllValidMoves(move1.State, nextPiece);

            float bestPly2Score = float.MinValue;
            foreach (var move2 in ply2Moves)
            {
                if (move2.Score > bestPly2Score)
                    bestPly2Score = move2.Score;
            }

            // Điểm tổng = Điểm nước 1 + Điểm tốt nhất của nước 2
            float totalScore = move1.Score + bestPly2Score;

            if (totalScore > globalBestScore)
            {
                globalBestScore = totalScore;
                bestPosition = move1.Position;
                bestRotation = move1.Rotation;
            }
        }
    }

    private BoardState GetCurrentBoardState()
    {
        BoardState state = new BoardState(Board.Size.x, Board.Size.y);
        int[,] data = board.BoardData;
        for (int y = 0; y < Board.Size.y; y++)
        {
            for (int x = 0; x < Board.Size.x; x++)
            {
                if (data[y, x] > 0) state.SetBit(x, y);
            }
        }
        return state;
    }

    private BoardState GetPredictBoardState()
    {
        // Dự đoán nơi current piece sẽ hạ cánh nếu rơi tự do từ vị trí hiện tại
        BoardState predictState = GetCurrentBoardState();
        int pieceIndex = board.TetrominoIndex;
        Vector2Int currentPos = board.PiecePoint;
        int currentRotation = board.PieceRotationIndex;

        Vector2Int[] tetromino = Tetrominoes.Get(pieceIndex, currentRotation);

        // Tìm vị trí cuối cùng khi piece rơi xuống
        int finalY = currentPos.y;
        while (IsValidPosition(predictState, currentPos.x, finalY - 1, tetromino))
        {
            finalY--;
        }

        // Đặt piece vào vị trí cuối cùng dự đoán
        int linesCleared = PlacePieceAndClearLines(ref predictState, currentPos.x, finalY, tetromino);

        return predictState;
    }

    private List<Move> GetAllValidMoves(BoardState state, int pieceIndex)
    {
        List<Move> validMoves = new List<Move>();

        for (int rot = 0; rot < 4; rot++)
        {
            Vector2Int[] tetromino = Tetrominoes.Get(pieceIndex, rot);

            // Bounds ngang thường nằm trong khoảng -2 đến Width + 2 tùy pivot
            for (int x = -2; x < state.Width + 2; x++)
            {
                // Kiểm tra xem vị trí x ở hàng trên cùng có hợp lệ không
                if (!IsValidPosition(state, x, state.Height - 1, tetromino)) continue;

                // Thả rơi tự do
                int y = state.Height - 1;
                while (IsValidPosition(state, x, y - 1, tetromino))
                {
                    y--;
                }

                // Simulate đặt block và xóa hàng
                BoardState newState = state.Clone();
                int linesCleared = PlacePieceAndClearLines(ref newState, x, y, tetromino);

                // Đánh giá điểm
                float score = Evaluate(newState, linesCleared);

                validMoves.Add(new Move
                {
                    Position = new Vector2Int(x, y),
                    Rotation = rot,
                    Score = score,
                    State = newState
                });
            }
        }
        return validMoves;
    }

    private bool IsValidPosition(BoardState state, int x, int y, Vector2Int[] tetromino)
    {
        foreach (var block in tetromino)
        {
            int tx = x + block.x;
            int ty = y + block.y;

            if (tx < 0 || tx >= state.Width || ty < 0) return false;
            if (ty < state.Height && state.GetBit(tx, ty)) return false;
        }
        return true;
    }

    private int PlacePieceAndClearLines(ref BoardState state, int x, int y, Vector2Int[] tetromino)
    {
        // Đặt block
        foreach (var block in tetromino)
        {
            int tx = x + block.x;
            int ty = y + block.y;
            if (ty < state.Height) state.SetBit(tx, ty);
        }

        // Xóa hàng bằng bitwise
        int linesCleared = 0;
        uint fullRowMask = (1u << state.Width) - 1;

        for (int r = 0; r < state.Height; r++)
        {
            if (state.Rows[r] == fullRowMask)
            {
                linesCleared++;
                // Dịch các hàng phía trên xuống
                for (int k = r; k < state.Height - 1; k++)
                {
                    state.Rows[k] = state.Rows[k + 1];
                }
                state.Rows[state.Height - 1] = 0;
                r--; // Kiểm tra lại hàng r hiện tại sau khi dịch xuống
            }
        }

        return linesCleared;
    }

    private float Evaluate(BoardState state, int linesCleared)
    {
        int aggregateHeight = 0;
        int holes = 0;
        int bumpiness = 0;

        int[] columnHeights = new int[state.Width];

        // Tính chiều cao từng cột và số lỗ hổng
        for (int x = 0; x < state.Width; x++)
        {
            bool blockFound = false;
            for (int y = state.Height - 1; y >= 0; y--)
            {
                if (state.GetBit(x, y))
                {
                    if (!blockFound)
                    {
                        columnHeights[x] = y + 1;
                        aggregateHeight += columnHeights[x];
                        blockFound = true;
                    }
                }
                else if (blockFound)
                {
                    holes++; // Ô trống nằm dưới ô có block là lỗ hổng
                }
            }
        }

        // Tính độ gồ ghề (Bumpiness)
        for (int x = 0; x < state.Width - 1; x++)
        {
            bumpiness += Mathf.Abs(columnHeights[x] - columnHeights[x + 1]);
        }

        // Hàm mục tiêu
        return (WeightHeight * aggregateHeight) +
               (WeightLines * linesCleared) +
               (WeightHoles * holes) +
               (WeightBumpiness * bumpiness);
    }
}