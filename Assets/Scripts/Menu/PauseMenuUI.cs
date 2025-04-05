using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MenuUI
{
    public static PauseMenuUI Instance { get; private set; }

    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {
        if(Instance)
        {
            Debug.LogWarning("PauseMenuUI already exists.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        resumeButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClickSound();
            Time.timeScale = 1.0f;
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
}
