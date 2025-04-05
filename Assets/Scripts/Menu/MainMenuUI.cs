using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MenuUI
{
    public static MainMenuUI Instance { get; private set; }

    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        if(Instance)
        {
            Debug.LogWarning("MainMenuUI already exists!");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        startButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClickSound();
            SelectModeMenuUI.Instance.Show(this);
            Hide(false);
        });

        quitButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayClickSound();
            YesNoMenuUI.Instance.Show(this);
            Hide(false);
        });
    }
    private void Start()
    {
        Show(null);
    }
}
