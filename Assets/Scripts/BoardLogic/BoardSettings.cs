using UnityEngine;

public class BoardSettings : MonoBehaviour
{
    public static BoardSettings Instance;

    public bool boardFlipped;
    
    public bool isWhiteHuman;
    public bool isBlackHuman;
    
    public bool debugLegalMoves;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(Instance);
        Instance = this;
    }
    
    
}
