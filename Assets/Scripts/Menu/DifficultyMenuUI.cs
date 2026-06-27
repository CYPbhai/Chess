using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DifficultyMenuUI : MenuUI
{
    public static DifficultyMenuUI Instance { get; private set; }

    [SerializeField] Button easyButton;
    [SerializeField] Button mediumButton;
    [SerializeField] Button hardButton;
    [SerializeField] Button backButton;

    private void Awake()
    {
        if (Instance)
        {
            Debug.LogWarning("DifficultyMenuUI already exists!");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        easyButton.onClick.AddListener(() =>
        {
            GameManager.Instance.Difficulty = AIDifficulty.Easy;
            HandleCommonCode();
        });

        mediumButton.onClick.AddListener(() =>
        {
            GameManager.Instance.Difficulty = AIDifficulty.Medium;
            HandleCommonCode();
        });

        hardButton.onClick.AddListener(() =>
        {
            GameManager.Instance.Difficulty = AIDifficulty.Hard;
            HandleCommonCode();
        });

        backButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClickSound();
            Hide(true);
        });
    }

    private void HandleCommonCode()
    {
        AudioManager.Instance.PlayClickSound();
        SceneManager.LoadScene("LoadingScene");
    }
    private void Start()
    {
        Hide(false);
    }
}
