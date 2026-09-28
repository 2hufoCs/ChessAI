using UnityEngine;

public class BoardSettings : MonoBehaviour
{
    public static BoardSettings Instance;

    public bool hasGameBegun;
    public bool boardFlipped;
    
    public bool isWhiteHuman;
    public bool isBlackHuman;
    
    public bool debugLegalMoves;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(Instance);
        Instance = this;

        hasGameBegun = true;
    }

    void OnEnable()
    {
        ActionsBus.OnCheckmate += (x) => hasGameBegun = false;
        ActionsBus.OnDraw += (x)  => hasGameBegun = false;
    }    
    
    void OnDisable()
    {
        ActionsBus.OnCheckmate -= (x) => hasGameBegun = false;
        ActionsBus.OnDraw -= (x)  => hasGameBegun = false;
    }
}
