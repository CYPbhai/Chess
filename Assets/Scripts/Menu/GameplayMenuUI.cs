using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayMenuUI : MenuUI
{
    public static GameplayMenuUI Instance {  get; private set; }

    [SerializeField] private Button pauseButton;
    [SerializeField] private TMP_Text turnText;

    private void Awake()
    {
        if(Instance)
        {
            Debug.LogWarning("GameplayMenuUI already exists");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        pauseButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClickSound();
            Time.timeScale = 0.0f;
            PauseMenuUI.Instance.Show(this);
            Hide(false);
        });
    }

    public void UpdateTurnText(bool isWhiteTurn)
    {
        if (isWhiteTurn)
        {
            turnText.text = "Turn: White";
            turnText.color = Color.white;
        }
        else
        {
            turnText.text = "Turn: Black";
            turnText.color = Color.black;
        }
    }

    private void Start()
    {
        Show(null);
    }
}
