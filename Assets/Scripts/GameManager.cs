using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private AIController aiController;
    public Board boardController;
    public bool canAction = true;
    public NextFrame[] nextFrames;
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
        levelText.text = "Level: " + currentLevel;
        scoreText.text = "Score: " + score;
    }

    // Update is called once per frame
    void Update()
    {
        // Nhấn chuột để chọn button
        if (Input.GetMouseButtonDown(0) && canAction)
        {
            Vector3 screenPosition = Input.mousePosition;
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
            ChooseButton(worldPosition);
        }
    }
    public void AddScore(int rowsCleared)
    {
        comboCount += 1;
        int scoreCombo = comboCount > 1 ? comboCount * 50 * currentLevel : 0; // Thêm điểm combo nếu comboCount > 1
        float scoreToAdd = Mathf.Pow(2, rowsCleared) * scorePerRow * currentLevel + scoreCombo;
        score += Mathf.RoundToInt(scoreToAdd);
        scoreText.text = "Score: " + score;

        linesCleared += rowsCleared;
        if (linesCleared >= 10)
        {
            currentLevel += 1;
            levelText.text = "Level: " + currentLevel;
            linesCleared = 0;

            boardController.dropTime = 0.8f * Mathf.Pow(0.9f, currentLevel - 1); // Tăng tốc độ rơi của piece theo level
        }
    }
    public void ChooseButton(Vector3 position)
    {
        Collider2D hitCollider = Physics2D.OverlapPoint(position, LayerMask.GetMask("Button"));

        if (hitCollider != null)
        {
            NextFrame nextFrame = hitCollider.GetComponent<NextFrame>();
            if (nextFrame != null)
            {
                // canAction = false;
                // HideNextFrame(nextFrame);
                //nextFrame.SpawnNextPiece();

                // boardController.SetNextPiece(nextFrame.GetNextPiece());
                boardController.nextPiece = nextFrame.GetNextPiece();
                boardController.HandDropPiece();

                // boardController.SpawnPiece(nextFrame.GetNextPiece());
            }
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

    public void ExecuteAIMove()
    {
        StartCoroutine(ExecuteAIMoveCoroutine());
    }

    private IEnumerator ExecuteAIMoveCoroutine()
    {
        aiController.GetBestMove(out Vector2Int bestPos, out int bestRot);

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

        yield return new WaitForSeconds(0.2f); // Delay trước khi drop

        // Drop piece
        // boardController.HandDropPiece();
    }
}
