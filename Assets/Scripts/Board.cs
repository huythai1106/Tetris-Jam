using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public static readonly Vector2Int Size = new(10, 20);
    [SerializeField] private Cell cellPrefab;
    [SerializeField] private Transform cellsTransform;
    [SerializeField] private NextFrame nextFrame;

    private readonly Cell[,] cells = new Cell[Size.y, Size.x];
    private readonly int[,] data = new int[Size.y, Size.x];

    private int tetrominoIndex;
    private Vector2Int piecePoint;
    private int pieceRotationIndex;
    public float dropTime = 0.8f;
    private float pieceDropTime = 0f;

    public readonly List<int> fullRows = new();
    private int topRowExclusive = 0;

    private Vector2Int ghostPoint;

    private void Start()
    {
        for (int r = 0; r < Size.y; r++)
        {
            for (int c = 0; c < Size.x; c++)
            {
                cells[r, c] = Instantiate(cellPrefab, cellsTransform);
                cells[r, c].transform.position = new Vector3Int(c, r, 0);
                cells[r, c].Hide();
            }
        }

        SpawnPiece(new Piece(Random.Range(0, Tetrominoes.Length), Random.Range(0, 4)));
        nextFrame.SpawnNextPiece();
    }

    private void Update()
    {
        pieceDropTime += Time.deltaTime;
        if (pieceDropTime >= dropTime)
        {
            pieceDropTime = 0;

            if (!DropPiece())
            {
                HideGhost();
                LockPiece();
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            MovePiece(Vector2Int.left);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            MovePiece(Vector2Int.right);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            MovePiece(Vector2Int.down);
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            RotatePiece();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            HandDropPiece();
        }
    }

    private void SpawnPiece(Piece piece)
    {
        tetrominoIndex = piece.TetrominoIndex;
        pieceRotationIndex = piece.RotationIndex;

        piecePoint = new(3, 17);

        pieceDropTime = 0f;

        ShowGhost();
        ShowPiece();

        if (!IsValidPiece(piecePoint, pieceRotationIndex))
        {
            Debug.Log("Game Over");
            enabled = false;
        }
    }

    private void MovePiece(Vector2Int direction)
    {
        var point = piecePoint + direction;
        if (!IsValidPiece(point, pieceRotationIndex)) return;

        HidePiece();
        HideGhost();

        piecePoint = point;

        ShowGhost();
        ShowPiece();
    }

    private void ShowPiece()
    {
        var tetromino = Tetrominoes.Get(tetrominoIndex, pieceRotationIndex);
        var pieceColor = Tetrominoes.Colors[tetrominoIndex];

        foreach (var p in tetromino)
        {
            cells[piecePoint.y + p.y, piecePoint.x + p.x].Show(pieceColor);
        }
    }

    private void RotatePiece()
    {
        var rotationIndex = (pieceRotationIndex + 1) % 4;
        if (!IsValidPiece(piecePoint, rotationIndex)) return;

        HidePiece();
        HideGhost();
        pieceRotationIndex = rotationIndex;
        ShowPiece();
        ShowGhost();
    }

    private void HidePiece()
    {
        var tetromino = Tetrominoes.Get(tetrominoIndex, pieceRotationIndex);

        foreach (var p in tetromino)
        {
            cells[piecePoint.y + p.y, piecePoint.x + p.x].Hide();
        }
    }

    public bool DropPiece()
    {
        var point = piecePoint + Vector2Int.down;
        if (!IsValidPiece(point, pieceRotationIndex)) return false;

        HidePiece();
        HideGhost();
        piecePoint = point;
        ShowPiece();
        ShowGhost();

        return true;
    }
    private void HandDropPiece()
    {
        HidePiece();
        HideGhost();
        while (IsValidPiece(piecePoint + Vector2Int.down, pieceRotationIndex))
        {
            piecePoint += Vector2Int.down;
        }

        LockPiece();
    }

    public void LockPiece()
    {
        var tetromino = Tetrominoes.Get(tetrominoIndex, pieceRotationIndex);

        foreach (var p in tetromino)
        {
            var lockPoint = piecePoint + p;
            data[lockPoint.y, lockPoint.x] = 1;
            cells[lockPoint.y, lockPoint.x].Show(Color.white);
        }

        topRowExclusive = Mathf.Min(Size.y, Mathf.Max(topRowExclusive, piecePoint.y + 4));

        ClearFullRows();

        SpawnPiece(nextFrame.GetNextPiece());
        nextFrame.SpawnNextPiece();
    }

    private bool IsValidPiece(Vector2Int point, int rotationIndex)
    {
        var tetromino = Tetrominoes.Get(tetrominoIndex, rotationIndex);

        foreach (var p in tetromino)
        {
            if (!IsValidPoint(point + p))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsValidPoint(Vector2Int point)
    {
        if (point.x < 0 || Size.x <= point.x) return false;
        if (point.y < 0 || Size.y <= point.y) return false;
        if (data[point.y, point.x] > 0) return false;

        return true;
    }

    private void ClearFullRows()
    {
        FullRows();
        if (fullRows.Count > 0)
        {
            foreach (var r in fullRows)
            {
                for (var c = 0; c < Size.x; c++)
                {
                    data[r, c] = 0;
                    cells[r, c].Hide();
                }
            }

            for (int i = 0; i < fullRows.Count - 1; i++)
            {
                for (var r = fullRows[i] + 1; r < fullRows[i + 1]; r++)
                {
                    DropRow(r, i + 1);
                }
            }

            for (var r = fullRows[^1] + 1; r < topRowExclusive; r++)
            {
                DropRow(r, fullRows.Count);
            }

            topRowExclusive -= fullRows.Count;
        }
    }

    private void DropRow(int row, int dropCount)
    {
        for (var c = 0; c < Size.x; c++)
        {
            if (data[row, c] > 0)
            {
                data[row - dropCount, c] = data[row, c];
                cells[row - dropCount, c].Show(Color.white);

                data[row, c] = 0;
                cells[row, c].Hide();
            }
        }
    }

    private void FullRows()
    {
        fullRows.Clear();
        var fromRow = Mathf.Max(0, piecePoint.y);
        var toRowExclusive = Mathf.Min(piecePoint.y + 4, Size.y);

        for (var r = fromRow; r < toRowExclusive; r++)
        {
            var isFullRow = true;
            for (var c = 0; c < Size.x; c++)
            {
                if (data[r, c] == 0)
                {
                    isFullRow = false;
                    break;
                }
            }

            if (isFullRow)
            {
                fullRows.Add(r);
            }
        }
    }
    public void ShowGhost()
    {
        ghostPoint = piecePoint;

        while (IsValidPiece(ghostPoint + Vector2Int.down, pieceRotationIndex))
        {
            ghostPoint += Vector2Int.down;
        }

        if (ghostPoint != piecePoint)
        {
            var tetromino = Tetrominoes.Get(tetrominoIndex, pieceRotationIndex);
            var pieceColor = Tetrominoes.Colors[tetrominoIndex];

            foreach (var p in tetromino)
            {
                cells[ghostPoint.y + p.y, ghostPoint.x + p.x].Ghost(pieceColor);
            }
        }
    }
    public void HideGhost()
    {
        var tetromino = Tetrominoes.Get(tetrominoIndex, pieceRotationIndex);

        foreach (var p in tetromino)
        {
            cells[ghostPoint.y + p.y, ghostPoint.x + p.x].Hide();
        }
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-1, topRowExclusive, 0), new Vector3(11, topRowExclusive, 0));
    }
}