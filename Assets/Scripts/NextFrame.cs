using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
    public bool isEnabled = true;
    [SerializeField] private Cell cellPrefab;
    [SerializeField] private Transform cellsTransform;
    // private readonly Cell[,] cells = new Cell[Size.y, Size.x];
    public int tetrominoIndex;
    private Vector2Int piecePoint;
    private int pieceRotationIndex;
    private Piece nextPiece;
    [SerializeField] private SpriteRenderer sprite;
    public Sprite[] hintSprites;
    Vector3 originalSpriteScale;

    // Start is called before the first frame update
    void Start()
    {
        // for (int r = 0; r < Size.y; r++)
        // {
        //     for (int c = 0; c < Size.x; c++)
        //     {
        //         cells[r, c] = Instantiate(cellPrefab, cellsTransform);
        //         cells[r, c].transform.localPosition = new Vector3Int(c, r, 0);
        //         cells[r, c].Hide();
        //     }
        // }

        // SpawnNextPiece();
        nextPiece = new Piece(tetrominoIndex, pieceRotationIndex);
        originalSpriteScale = sprite.transform.localScale;
    }
    int countChoose = 0;
    public Piece GetNextPiece()
    {
        if (isEnabled)
        {
            countChoose += 1;
            if (countChoose >= 3)
            {
                SetEnable(false);
            }
        }

        return nextPiece;
    }
    public void SetCountChoose(int count)
    {
        countChoose = count;
    }
    public void SetEnable(bool value)
    {
        isEnabled = value;
        sprite.color = new Color(1, 1, 1, value ? 1f : 0.3f);
        if (!value)
        {
            countChoose = 0;
        }
    }
    public void SpawnNextPiece()
    {
        HidePiece();
        // nextPiece = new Piece(tetrominoIndex, pieceRotationIndex);

        // tetrominoIndex = setSpawn != -1 ? setSpawn : Random.Range(0, Tetrominoes.Length);
        // pieceRotationIndex = Random.Range(0, 4);
        // pieceRotationIndex = 0;

        // piecePoint = new(0, 0);
        // nextPiece = new Piece(tetrominoIndex, pieceRotationIndex);

        // ShowPiece();
    }
    public void SetAnimScale()
    {
        sprite.transform.DOScale(originalSpriteScale * 1.3f, 0.3f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    }
    public void StopAnimScale()
    {
        sprite.transform.DOKill();
        sprite.transform.localScale = originalSpriteScale;
    }
    private void ShowPiece()
    {
        // var tetromino = Tetrominoes.Get(tetrominoIndex, pieceRotationIndex);
        // var pieceColor = Tetrominoes.Colors[tetrominoIndex];

        // foreach (var p in tetromino)
        // {
        //     cells[piecePoint.y + p.y, piecePoint.x + p.x].Show(pieceColor);
        // }
    }
    public void HidePiece()
    {
        // var tetromino = Tetrominoes.Get(tetrominoIndex, pieceRotationIndex);

        // foreach (var p in tetromino)
        // {
        //     cells[piecePoint.y + p.y, piecePoint.x + p.x].Hide();
        // }
    }

    public void CheckSpam()
    {

    }
}
