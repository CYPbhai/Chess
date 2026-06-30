using UnityEngine;
public enum AIDifficulty
{
    Easy,
    Medium,
    Hard
}

public enum AIType 
{ 
    CustomMinimax, 
    Stockfish 
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private bool isTwoPlayer;
    private bool isPlayerWhite;
    private AIDifficulty difficulty = AIDifficulty.Medium;
    private bool isAIvsAI;
    private AIType _aiType = AIType.CustomMinimax;
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
    public AIDifficulty Difficulty
    {
        get { return difficulty; }
        set { difficulty = value; }
    }
    public bool IsAIvsAI
    {
        get { return isAIvsAI; }
        set { isAIvsAI = value; }
    }    

    public AIType aiType
    {
        get { return _aiType; }
        set { _aiType = value; }
    }
}
