using System.Collections.Generic;
using UnityEngine;

public class LegalMoveLogic : MonoBehaviour
{
    public static LegalMoveLogic Instance { get;  private set; }

    [HideInInspector] public Dictionary<Piece, List<Vector2Int>> whiteTargetedSquares = new();
    [HideInInspector] public Dictionary<Piece, List<Vector2Int>> blackTargetedSquares = new();
    
    [HideInInspector] public Dictionary<Piece, List<Move>> whitePseudolegalMoves = new();
    [HideInInspector] public Dictionary<Piece, List<Move>> blackPseudolegalMoves = new();

    [HideInInspector] public Dictionary<GameObject, Piece> goToPieces = new();


    [HideInInspector] public Piece heldPiece;
    [HideInInspector] public Vector2Int heldPieceStartSquare;
    [HideInInspector] public List<Move> heldPieceLegalMoves = new();
    
    [Header("Debug")]
    [SerializeField] private GameObject _squareHighlighterPrefab;
    [SerializeField] private GameObject _targetedSquareHighlighterPrefab;
    
    private readonly List<GameObject> _squareHighlighters = new();
    private readonly List<GameObject> _targetedSquaresHighlighters = new();

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(Instance);
        Instance = this;
    }
    
    public void ShowLegalMoves()
    {
        HideLegalMoves();
        foreach (Move move in heldPieceLegalMoves)
        {
            GameObject go = Instantiate(_squareHighlighterPrefab, BoardToWorld(move.endSquare), Quaternion.identity, transform);
            _squareHighlighters.Add(go);
        }
    }

    public void HideLegalMoves()
    {
        // Destroy previous square highlighters
        foreach (GameObject go in _squareHighlighters)
            Destroy(go);
        _squareHighlighters.Clear();
    }

    public Move FindPseudoLegalMove(Vector2Int endSquare)
    {
        foreach (Move move in heldPieceLegalMoves)
        {
            if (move.endSquare == endSquare)
                return move;
        }
        return new Move { endSquare = -Vector2Int.one };
    }
    
    Vector2 BoardToWorld(Vector2 pos)
    {
        return pos + (Vector2)transform.position - Vector2.one * 3.5f;
    }
}
