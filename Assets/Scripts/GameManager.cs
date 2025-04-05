using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private bool isTwoPlayer;
    private bool isPlayerWhite;
    private void Awake()
    {
        if(Instance)
        {
            Debug.LogWarning("GameManager already exists!");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsTwoPlayer
    {
        get { return isTwoPlayer; }
        set { isTwoPlayer = value; }
    }

    public bool IsPlayerWhite
    {
        get { return isPlayerWhite; }
        set { isPlayerWhite = value; }
    }
}
