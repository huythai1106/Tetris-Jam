using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public struct NextMove
{
    public Vector2Int pos;
    public int rot;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private AIController aiController;
    public Board boardController;
    public bool canAction = true;
    bool isPaused = false;
    public NextFrame[] nextFrames;
    private float lastClickTime;
    private const float doubleClickThreshold = 0.3f;
    [Header("LevelControl")]
    public int currentLevel = 1;
    int linesCleared = 0;
    [Header("Score")]
    public int score = 0;
    public int scorePerRow = 100;
    public int comboCount = 0;
    [Header("UI")]
    public Text levelText;
    public Text scoreText;
    public NextMove nextMove;
    public SpriteRenderer[] hintRenderers;
    public ShowFrame[] hintShowFrames;

    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;
    }
    void OnDestroy()
    {
        Instance = null;
    }
    void Start()
    {
        //SpawnNextPiece();
        levelText.text = currentLevel.ToString();
        scoreText.text = score.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        // Nhấn chuột để chọn button
        if (Input.GetMouseButtonDown(0) && canAction)
        {
            Vector3 screenPosition = Input.mousePosition;
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);

            float timeSinceLastClick = Time.time - lastClickTime;

            if (timeSinceLastClick <= doubleClickThreshold)
            {
                // ĐÂY LÀ DOUBLE CLICK
                ChooseButton(worldPosition, true);
            }
            else
            {
                // ĐÂY LÀ SINGLE CLICK (Hoặc lần click đầu tiên)
                ChooseButton(worldPosition, false);
            }

            lastClickTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
    }
    public void AddScore(int rowsCleared)
    {
        comboCount += 1;
        int scoreCombo = comboCount > 1 ? comboCount * 50 * currentLevel : 0; // Thêm điểm combo nếu comboCount > 1
        float scoreToAdd = Mathf.Pow(2, rowsCleared) * scorePerRow * currentLevel + scoreCombo;
        score += Mathf.RoundToInt(scoreToAdd);
        scoreText.text = score.ToString();

        linesCleared += rowsCleared;
        if (linesCleared >= 6)
        {
            currentLevel += 1;
            levelText.text = currentLevel.ToString();
            linesCleared = 0;

            boardController.dropTime = 0.8f * Mathf.Pow(0.9f, currentLevel - 1); // Tăng tốc độ rơi của piece theo level
        }
    }
    public void ChooseButton(Vector3 position, bool isDoubleClick = false)
    {
        Collider2D hitCollider = Physics2D.OverlapPoint(position, LayerMask.GetMask("Button"));

        if (hitCollider != null)
        {
            NextFrame nextFrame = hitCollider.GetComponent<NextFrame>();
            if (nextFrame != null && nextFrame.isEnabled)
            {
                // canAction = false;
                // HideNextFrame(nextFrame);
                //nextFrame.SpawnNextPiece();
                // boardController.SetNextPiece(nextFrame.GetNextPiece());
                if (isDoubleClick)
                {
                    ResetNextFrames(nextFrame);
                    boardController.nextPiece = nextFrame.GetNextPiece();
                    lastChosenFrame = nextFrame;
                    boardController.HandDropPiece();
                }
                else
                {
                    ShowDetailHint(nextFrame.tetrominoIndex);
                    ResetAnimScale();
                    nextFrame.SetAnimScale();
                }
                // boardController.SpawnPiece(nextFrame.GetNextPiece());
            }
        }
    }
    NextFrame lastChosenFrame;
    public void ResetNextFrames(NextFrame exceptFrame)
    {
        if (lastChosenFrame != null && lastChosenFrame != exceptFrame)
        {
            lastChosenFrame.SetCountChoose(0);
        }

        foreach (NextFrame next in nextFrames)
        {
            if (!next.isEnabled)
            {
                next.SetEnable(true);
            }
        }
    }
    public void ResetAnimScale()
    {
        foreach (NextFrame next in nextFrames)
        {
            next.StopAnimScale();
        }
    }
    public void ShowDetailHint(int tetrominoIndex)
    {
        // for (int i = 0; i < hintRenderers.Length; i++)
        // {
        //     hintRenderers[i].gameObject.SetActive(false);
        // }

        // for (int i = 0; i < hintSprites.Length; i++)
        // {
        //     hintRenderers[i].gameObject.SetActive(true);
        //     hintRenderers[i].sprite = hintSprites[i];
        // }
        foreach (var item in hintShowFrames)
        {
            item.ShowPiece(tetrominoIndex);
        }
    }
    public void HideDetailHint()
    {
        foreach (var item in hintShowFrames)
        {
            item.HidePiece();
        }
    }

    public void HideNextFrame(NextFrame nextFrame)
    {
        foreach (NextFrame next in nextFrames)
        {
            if (next != nextFrame)
                next.HidePiece();
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0 : 1;
    }

    public void ExecuteAIMove()
    {
        StartCoroutine(ExecuteAIMoveCoroutine());
    }

    private IEnumerator ExecuteAIMoveCoroutine()
    {
        // aiController.GetBestMove(out Vector2Int bestPos, out int bestRot);
        Vector2Int bestPos = nextMove.pos;
        int bestRot = nextMove.rot;

        // Rotate piece to best rotation
        int currentRot = boardController.PieceRotationIndex;
        int rotDiff = (bestRot - currentRot + 4) % 4;
        for (int i = 0; i < rotDiff; i++)
        {
            boardController.AIRotatePiece();
            yield return new WaitForSeconds(0.1f); // Delay 100ms giữa các lần rotate
        }

        // Move piece to best X position
        int currentX = boardController.PiecePoint.x;
        int moveDiff = bestPos.x - currentX;

        if (moveDiff > 0)
        {
            for (int i = 0; i < moveDiff; i++)
            {
                boardController.AIMovePiece(Vector2Int.right);
                yield return new WaitForSeconds(0.1f); // Delay 50ms giữa các lần di chuyển
            }
        }
        else if (moveDiff < 0)
        {
            for (int i = 0; i < -moveDiff; i++)
            {
                boardController.AIMovePiece(Vector2Int.left);
                yield return new WaitForSeconds(0.1f); // Delay 50ms giữa các lần di chuyển
            }
        }

        GenerateNextMove();

        yield return new WaitForSeconds(0.2f); // Delay trước khi drop
        // Drop piece
        // boardController.HandDropPiece();
    }

    public void GenerateNextMove()
    {
        aiController.GetBestMove(out Vector2Int bestPos, out int bestRot);
        nextMove = new()
        {
            pos = bestPos,
            rot = bestRot
        };

        boardController.SetInfo(bestPos, bestRot);
        Debug.Log("Next Move - Position: " + bestPos + ", Rotation: " + bestRot);
    }
}
