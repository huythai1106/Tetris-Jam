using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece
{
    public int TetrominoIndex { get; private set; }
    public int RotationIndex { get; private set; }

    public Piece(int tetrominoIndex, int rotationIndex)
    {
        TetrominoIndex = tetrominoIndex;
        RotationIndex = rotationIndex;
    }
}

public class NextFrame : MonoBehaviour
{
    public static readonly Vector2Int Size = new(4, 4);
    [SerializeField] private Cell cellPrefab;
    [SerializeField] private Transform cellsTransform;
    private readonly Cell[,] cells = new Cell[Size.y, Size.x];
    public int tetrominoIndex;
    private Vector2Int piecePoint;
    private int pieceRotationIndex;
    private Piece nextPiece;

    // Start is called before the first frame update
    void Start()
    {
        for (int r = 0; r < Size.y; r++)
        {
            for (int c = 0; c < Size.x; c++)
            {
                cells[r, c] = Instantiate(cellPrefab, cellsTransform);
                cells[r, c].transform.localPosition = new Vector3Int(c, r, 0);
                cells[r, c].Hide();
            }
        }

        SpawnNextPiece();

    }
    public Piece GetNextPiece()
    {
        return nextPiece;
    }
    public void SpawnNextPiece()
    {
        HidePiece();

        // tetrominoIndex = setSpawn != -1 ? setSpawn : Random.Range(0, Tetrominoes.Length);
        // pieceRotationIndex = Random.Range(0, 4);
        pieceRotationIndex = 0;

        piecePoint = new(0, 0);
        nextPiece = new Piece(tetrominoIndex, pieceRotationIndex);

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
    public void HidePiece()
    {
        var tetromino = Tetrominoes.Get(tetrominoIndex, pieceRotationIndex);

        foreach (var p in tetromino)
        {
            cells[piecePoint.y + p.y, piecePoint.x + p.x].Hide();
        }
    }

    public void CheckSpam()
    {

    }
}
