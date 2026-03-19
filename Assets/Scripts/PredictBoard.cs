using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredictBoard : MonoBehaviour
{
    public static readonly Vector2Int Size = new(10, 20);
    [SerializeField] private Cell cellPrefab;
    [SerializeField] private Transform cellsTransform;

    private readonly Cell[,] cells = new Cell[Size.y, Size.x];
    private readonly int[,] data = new int[Size.y, Size.x];
}
