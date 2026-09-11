using System.Collections.Generic;
using NUnit.Framework.Constraints;
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

    public void RemovePieceFromLegalMoves(Piece piece)
    {
        if (piece.pieceData.color == PieceColor.White)
        {
            whitePseudolegalMoves.Remove(piece);
            whiteTargetedSquares.Remove(piece);
            return;
        }
        blackPseudolegalMoves.Remove(piece);
        blackTargetedSquares.Remove(piece);
    }

    public void AddPieceToLegalMoves(Piece piece)
    {
        List<Move> moves = piece.GetPseudolegalMoves(WorldToBoard(piece.go.transform.position));
        List<Vector2Int> positions = new();
        foreach (Move move in moves)
            positions.Add(move.endSquare);
            
        if (piece.pieceData.color == PieceColor.White)
        {
            whitePseudolegalMoves.Add(piece, moves);
            whiteTargetedSquares.Add(piece, positions);
            Debug.Log("white queen legal moves: " + whitePseudolegalMoves[piece].Count);
            return;
        }
        blackPseudolegalMoves.Add(piece, moves);
        blackTargetedSquares.Add(piece, positions);
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
    
    public void ComputeTargetedSquares(Dictionary<Vector2, Piece> pieces)
    {
        // Reinitialize lists
        whiteTargetedSquares.Clear();
        blackTargetedSquares.Clear();
        whitePseudolegalMoves.Clear();
        blackPseudolegalMoves.Clear();
        
        foreach (KeyValuePair<Vector2, Piece> piece in pieces)
        {
            // Get legal moves of piece, track targeted squares
            List<Move> moves = piece.Value.GetPseudolegalMoves(piece.Key);
            List<Vector2Int> targetPositions = new();
            foreach (Move move in moves)
                targetPositions.Add(move.endSquare);
            
            if (piece.Value.pieceData.color == PieceColor.White)
            {
                whiteTargetedSquares.Add(piece.Value, targetPositions);
                whitePseudolegalMoves.Add(piece.Value, moves);
            }
            else
            {
                blackTargetedSquares.Add(piece.Value, targetPositions);
                blackPseudolegalMoves.Add(piece.Value, moves);
            }
        }
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
    
    Vector2 WorldToBoard(Vector2 pos)
    {
        Vector2 snappedPos = new Vector2(pos.x > 0 ? (int)pos.x + 1 : (int)pos.x, pos.y > 0 ? (int)pos.y + 1 : (int)pos.y);
        return snappedPos + (Vector2)transform.position + Vector2.one * 3;
    }
}
