using UnityEngine;

public class AIController : MonoBehaviour
{
    [SerializeField] private Board board;
    [SerializeField] private bool enableAI = true;
    [SerializeField] private float moveDelay = 0.1f; // Độ trễ giữa các lệnh di chuyển (để nhìn rõ AI đang làm gì)

    private TetrisAI ai;
    private float moveTimer = 0f;
    private TetrisAI.Placement currentPlacement;
    private bool isExecutingPlacement = false;
    private int targetRotation = 0;
    private int targetX = 0;

    private void Start()
    {
        if (board == null)
        {
            board = GetComponent<Board>();
        }

        ai = new TetrisAI(Board.Size.x, Board.Size.y);
    }

    private void Update()
    {
        if (!enableAI || board == null) return;

        // Tìm placement tốt nhất khi piece mới spawn
        if (!isExecutingPlacement)
        {
            FindAndExecuteBestPlacement();
        }
        else
        {
            // Thực thi placement: di chuyển và xoay
            moveTimer += Time.deltaTime;
            if (moveTimer >= moveDelay)
            {
                moveTimer = 0f;
                ExecutePlacementStep();
            }
        }
    }

    private void FindAndExecuteBestPlacement()
    {
        // Lấy thông tin piece hiện tại từ Board qua public properties
        currentPlacement = ai.FindBestPlacement(
            board.TetrominoIndex,
            board.PieceRotationIndex,
            board.PiecePoint,
            board.BoardData
        );

        targetRotation = currentPlacement.RotationIndex;
        targetX = currentPlacement.Position.x;
        isExecutingPlacement = true;
    }

    private void ExecutePlacementStep()
    {
        int currentX = board.PiecePoint.x;
        int currentRotation = board.PieceRotationIndex;

        // Bước 1: Xoay piece về rotation đích
        if (currentRotation != targetRotation)
        {
            board.AIRotatePiece();
            return;
        }

        // Bước 2: Di chuyển piece đến X đích
        if (currentX < targetX)
        {
            board.AIMovePiece(Vector2Int.right);
        }
        else if (currentX > targetX)
        {
            board.AIMovePiece(Vector2Int.left);
        }
        else
        {
            // Đã ở vị trí đích, để piece rơi xuống tự động
            isExecutingPlacement = false;
        }
    }

    // Helper method để gọi các private method của Board qua Reflection
    private void CallBoardMethod(string methodName, Vector2Int direction = default)
    {
        var method = board.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method != null)
        {
            if (direction == default)
            {
                method.Invoke(board, null);
            }
            else
            {
                method.Invoke(board, new object[] { direction });
            }
        }
    }

    // Lấy dữ liệu từ Board (cần expose qua public properties hoặc reflection)
    private int GetTetrominoIndex()
    {
        var field = board.GetType().GetField("tetrominoIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (int)field.GetValue(board) : 0;
    }

    private int GetPieceRotationIndex()
    {
        var field = board.GetType().GetField("pieceRotationIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (int)field.GetValue(board) : 0;
    }

    private Vector2Int GetPiecePosition()
    {
        var field = board.GetType().GetField("piecePoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (Vector2Int)field.GetValue(board) : Vector2Int.zero;
    }

    private int[,] GetBoardData()
    {
        var field = board.GetType().GetField("data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (int[,])field.GetValue(board) : new int[Board.Size.y, Board.Size.x];
    }

    public void ToggleAI()
    {
        enableAI = !enableAI;
    }
}
