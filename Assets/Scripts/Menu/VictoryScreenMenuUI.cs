using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictoryScreenMenuUI : MenuUI
{
    public static VictoryScreenMenuUI Instance { get; private set; }
    [SerializeField] private ChessBoard chessBoard;
    [SerializeField] private TMP_Text victoryText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    private void Awake()
    {
        if(Instance)
        {
            Debug.LogWarning("VictoryScreenMenuUI already exists");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        restartButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClickSound();
            chessBoard.OnRestartButton();
            UpdateVictoryText("");
            Hide(true);
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClickSound();
            YesNoMenuUI.Instance.Show(this);
            Hide(false);
        });
    }

    private void Start()
    {
        Hide(false);
    }

    public void UpdateVictoryText(string str)
    {
        victoryText.text = str;
    }
}
