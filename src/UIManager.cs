using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject pausedPanel;
    [SerializeField] private TextMeshProUGUI centreText;
    [SerializeField] private GameObject[] livesArray;
    [SerializeField] private TextMeshProUGUI scorePopupText;
    [SerializeField] private GameObject gameOverPanel;

    private Canvas canvas;
    private GameManager gameManager;

    private uint lastScore;
    private uint lastLevel;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.FindObjectOfType<GameManager>();
        canvas = GameObject.FindObjectOfType<Canvas>();

        gameManager.LevelReset += ResetGameUI;
        gameManager.LevelStarted += DisableReadyText;
        gameManager.ScoreUpdated += UpdateScoreText;
        gameManager.LevelChanged += UpdateLevelText;
        gameManager.PausedStateChange += TogglePausedText;
        gameManager.LivesChanged += UpdateLivesIcons;
        gameManager.EnemyDefeated += ShowEnemyScorePopup;
        gameManager.GameOver += ShowGameOverScreen;

        lastScore = 0;
        lastLevel = 1;

        ResetGameUI();
    }

    private void ResetGameUI() {
        UpdateScoreText(lastScore);
        UpdateLevelText(lastLevel);
        centreText.gameObject.SetActive(true);
        gameOverPanel.SetActive(false);
        pausedPanel.SetActive(false);
    }

    private void DisableReadyText() {
        centreText.gameObject.SetActive(false);
    }

    private void UpdateScoreText(uint newScore)
    {
        scoreText.text = newScore.ToString();
        lastScore = newScore;
    }

    private void UpdateLevelText(uint newLevel)
    {
        levelText.text = newLevel.ToString();
        lastLevel = newLevel;
    }

    private void TogglePausedText(bool paused)
    {
        pausedPanel.SetActive(paused);

        if (!paused)
            scorePopupText.gameObject.SetActive(false);
    }

    private void UpdateLivesIcons(int numLives)
    {
        Debug.Assert(numLives <= livesArray.Length);

        for (int i = 0; i < livesArray.Length; ++i)
        {
            if (i < numLives)
                livesArray[i].SetActive(true);
            else
                livesArray[i].SetActive(false);
        }
    }

    private void ShowEnemyScorePopup(Vector3 pos, uint score)
    {
        scorePopupText.text = score.ToString();
        scorePopupText.gameObject.SetActive(true);

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        Vector2 viewportPoint = (Vector2)Camera.main.WorldToViewportPoint(pos);
        Vector2 offset = canvasRect.sizeDelta/2;

        Vector2 screenPoint = viewportPoint*canvasRect.sizeDelta - offset;

        RectTransform rectTransform = scorePopupText.GetComponent<RectTransform>();
        rectTransform.localPosition = screenPoint;
    }

    private void ShowGameOverScreen()
    {
        gameOverPanel.SetActive(true);
    }

    public void RestartOnClick()
    {
        gameManager.ResetGame();
    }

    public void TitleScreenOnClick()
    {
        SceneManager.LoadScene("Title Screen");
    }
}
