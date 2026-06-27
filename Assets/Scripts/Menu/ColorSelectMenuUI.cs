using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ColorSelectMenuUI : MenuUI
{
    public static ColorSelectMenuUI Instance { get; private set; }

    [SerializeField] private Button whiteButton;
    [SerializeField] private Button blackButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        if (Instance)
        {
            Debug.LogWarning("ColorSelectMenuUI already exists!");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        whiteButton.onClick.AddListener(() =>
        {
            GameManager.Instance.IsPlayerWhite = true;
            HandleCommonCode();
        });

        blackButton.onClick.AddListener(() =>
        {
            GameManager.Instance.IsPlayerWhite = false;
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
        DifficultyMenuUI.Instance.Show(this);
        Hide(false);
    }

    private void Start()
    {
        Hide(false);
    }
}
