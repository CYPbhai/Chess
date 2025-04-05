using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectModeMenuUI : MenuUI
{
    public static SelectModeMenuUI Instance { get; private set; }

    [SerializeField] private Button singlePlayerButton;
    [SerializeField] private Button twoPlayerButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        if (Instance)
        {
            Debug.LogWarning("SelectModeMenuUI already exists!");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        singlePlayerButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClickSound();
            GameManager.Instance.IsTwoPlayer = false;
            ColorSelectMenuUI.Instance.Show(this);
            Hide(false);
        });

        twoPlayerButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClickSound();
            GameManager.Instance.IsTwoPlayer = true;
            GameManager.Instance.IsPlayerWhite = true;
            SceneManager.LoadScene("LoadingScene");
        });

        backButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClickSound();
            Hide(true);
        });
    }

    private void Start()
    {
        Hide(false);
    }
}
