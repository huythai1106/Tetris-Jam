using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowFrame : MonoBehaviour
{
    public static readonly Vector2Int Size = new(4, 4);
    [SerializeField] private Cell cellPrefab;
    [SerializeField] private Transform cellsTransform;
    private readonly Cell[,] cells = new Cell[Size.y, Size.x];
    private Vector2Int piecePoint;
    public int pieceRotationIndex;
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
    }

    public void ShowPiece(int tetrominoIndex)
    {
        HidePiece();

        var tetromino = Tetrominoes.Get(tetrominoIndex, pieceRotationIndex);
        var pieceColor = Tetrominoes.Colors[tetrominoIndex];

        foreach (var p in tetromino)
        {
            cells[p.y, p.x].Show(pieceColor);
        }
    }
    public void HidePiece()
    {
        for (int r = 0; r < Size.y; r++)
        {
            for (int c = 0; c < Size.x; c++)
            {
                Cell cell = cellsTransform.GetChild(r * Size.x + c).GetComponent<Cell>();
                cell.Hide();
            }
        }
    }
}
