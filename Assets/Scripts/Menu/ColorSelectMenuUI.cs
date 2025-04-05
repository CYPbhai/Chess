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
            AudioManager.Instance.PlayClickSound();
            GameManager.Instance.IsPlayerWhite = true;
            SceneManager.LoadScene("LoadingScene");
        });

        blackButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClickSound();
            GameManager.Instance.IsPlayerWhite = false;
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
