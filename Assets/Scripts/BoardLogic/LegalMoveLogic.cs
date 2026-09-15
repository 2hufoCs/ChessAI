using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Constraints;
using Pieces;
using UnityEngine;

public class LegalMoveGenerator : MonoBehaviour
{
    public static LegalMoveGenerator Instance { get;  private set; }

    [HideInInspector] public Dictionary<Piece, List<Vector2Int>> whiteTargetedSquares = new();
    [HideInInspector] public Dictionary<Piece, List<Vector2Int>> blackTargetedSquares = new();
    
    Dictionary<Piece, List<Move>> whitePseudolegalMoves = new();
    Dictionary<Piece, List<Move>> blackPseudolegalMoves = new();    
    
    public Dictionary<Piece, List<Move>> whiteLegalMoves = new();
    public Dictionary<Piece, List<Move>> blackLegalMoves = new();

    [HideInInspector] public Dictionary<GameObject, Piece> goToPieces = new();
    
    // Used to deal with checks
    private bool _canBlockCheck;
    private bool _canTakeChecker;
    private List<Vector2Int> _blockCheckSquares = new();
    private List<Vector2Int> _lookThroughKingSquares = new();
    
    // Used to deal with pins
    public static Dictionary<Piece, List<Vector2Int>> pins = new();
    
    // For human play
    [HideInInspector] public Piece heldPiece;
    [HideInInspector] public Vector2Int heldPieceStartSquare;
    [HideInInspector] public List<Move> heldPieceLegalMoves = new();
    
    [Header("Debug")]
    [SerializeField] private GameObject _squareHighlighterPrefab;
    [SerializeField] private GameObject _targetedSquareHighlighterPrefab;
    [SerializeField] private GameObject _blockOptionHighlighterPrefab;
    [SerializeField] private GameObject _pinHighlighterPrefab;
    
    private readonly List<GameObject> _squareHighlighters = new();
    private readonly List<GameObject> _targetedSquaresHighlighters = new();
    private readonly List<GameObject> _blockOptionsHighlighters = new();
    private readonly List<GameObject> _pinHighlighters = new();

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(Instance);
        Instance = this;
    }

    public void GetAllLegalMoves(Dictionary<Vector2, Piece> pieces, King kingToPlay)
    {
        pins = new();
        Dictionary<Piece, List<Move>> pseudoLegalMoves = GetPseudoLegalMoves(pieces, kingToPlay);
        Dictionary<Piece, List<Move>> legalMoves = new();
        
        bool isWhitePlaying = kingToPlay.pieceData.color == PieceColor.White;
        
        // Iterate through every piece that has pseudo legal moves
        foreach (KeyValuePair<Piece, List<Move>> pieceMoves in pseudoLegalMoves)
        {
            // For pieces that are pinned
            List<Move> movesWithoutPins = new();
            
            // For whatever reason, this doesn't work
            if (pins.TryGetValue(goToPieces[pieceMoves.Key.go], out List<Vector2Int> posToRemove))
            {
                // Piece is located in pins dict, only allow moves inside list
                foreach (Move move in pieceMoves.Value)
                {
                    if (pins[pieceMoves.Key].Contains(move.endSquare))
                        movesWithoutPins.Add(move);
                }
            }
            else movesWithoutPins = new(pieceMoves.Value);
            //Debug.Log($"for {pieceMoves.Key.pieceData.color} {pieceMoves.Key.pieceData.type}, how many moves including pins: {movesWithoutPins.Count}");
            
            if (!kingToPlay.isInCheck && pieceMoves.Key != kingToPlay)
            {
                legalMoves[pieceMoves.Key] = movesWithoutPins;
                continue;
            }
            
            List<Move> newMoves = new();
            foreach (Move move in movesWithoutPins)
            {
                // If piece is king, simply need to move to non-targeted square
                if (pieceMoves.Key == kingToPlay)
                {
                    if (!IsSquareTargeted(move.endSquare, kingToPlay.pieceData.color) && !_lookThroughKingSquares.Contains(move.endSquare))
                        newMoves.Add(move);
                    continue;
                }

                if (!_canBlockCheck || !kingToPlay.isInCheck)
                {
                    if (_blockCheckSquares[0] == move.endSquare)    
                        newMoves.Add(move);
                    continue;
                }
                    
                
                // For other pieces, must reach one of block options
                if (_blockCheckSquares.Contains(move.endSquare))
                    newMoves.Add(move);
            }
            legalMoves.Add(pieceMoves.Key, newMoves);
        }

        if (isWhitePlaying)
            whiteLegalMoves =  new(legalMoves);
        else blackLegalMoves = new(legalMoves);
    }
    
    public Dictionary<Piece, List<Move>> GetPseudoLegalMoves(Dictionary<Vector2, Piece> pieces, King targetKing)
    {
        // Reinitialize lists
        whiteTargetedSquares.Clear();
        blackTargetedSquares.Clear();
        whitePseudolegalMoves.Clear();
        blackPseudolegalMoves.Clear();
        
        // Reset a bunch of stuff
        int enemyCheckCount = 0;
        targetKing.isInCheck = false;
        _canBlockCheck = true;
        _canTakeChecker = true;
        
        _blockCheckSquares.Clear();
        _lookThroughKingSquares.Clear();
        
        foreach (KeyValuePair<Vector2, Piece> piece in pieces)
        {
            // Get legal moves of piece, track targeted squares
            List<Move> moves = new();
            List<Vector2Int> targetPositions = new();
            foreach (Move move in piece.Value.GetPseudolegalMoves(piece.Key, true))
            {
                if (move.isMoveLegal)
                    moves.Add(move);
                targetPositions.Add(move.endSquare);
            }
            
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

            Vector2Int kingPos = Vector2Int.RoundToInt(WorldToBoard(targetKing.go.transform.position));
            if (piece.Value.pieceData.color == targetKing.pieceData.color || !targetPositions.Contains(kingPos))
                continue;
            
            // King is in check, verify if player can block or not
            targetKing.isInCheck = true;
            enemyCheckCount++;
            _canBlockCheck &= enemyCheckCount < 2;
            _canTakeChecker &= enemyCheckCount < 2;
            
            GetKingLinesOfAttack(pieces, targetKing, piece);
        }
        
        Dictionary<Piece, List<Move>> result = targetKing.pieceData.color == PieceColor.White ? whitePseudolegalMoves : blackPseudolegalMoves;
        if (BoardSettings.Instance.debugLegalMoves)
        {
            ShowTargetedSquares(targetKing);
            ShowBlockOptions();
            ShowPins();
        }
        return result;
    }

    void GetKingLinesOfAttack(Dictionary<Vector2, Piece> pieces, King targetKing, KeyValuePair<Vector2, Piece> piece)
    {
        Vector2Int basePos = Vector2Int.RoundToInt(piece.Key);
        Vector2Int kingPos = Vector2Int.RoundToInt(WorldToBoard(targetKing.go.transform.position));
        _blockCheckSquares.Add(basePos);
            
        PieceType enemyType = piece.Value.pieceData.type;
        if (enemyType == PieceType.Bishop || enemyType == PieceType.Rook || enemyType == PieceType.Queen)
        {
            // Get direction of line of attack
            Vector2Int posDiff = kingPos - basePos;
            Vector2Int clampedDiff = new Vector2Int(Mathf.Clamp(posDiff.x, -1, 1), Mathf.Clamp(posDiff.y, -1, 1));
            
            // Get all squares in line of attack
            Vector2Int squareToBlock = basePos;
            for (int i = 1; squareToBlock != kingPos; i++)
            {
                if (i > 8)
                {
                    Debug.LogError("couldn't get all squares in line of attack towards the king, prevented infinite loop");
                    break;
                }

                squareToBlock += clampedDiff;
                _blockCheckSquares.Add(squareToBlock);
            }
            
            _lookThroughKingSquares.Add(kingPos + clampedDiff);
        }
        else
            _canBlockCheck = false;
    }

    public bool IsSquareTargeted(Vector2Int pos, PieceColor friendlyColor)
    {
        Dictionary<Piece, List<Vector2Int>> enemyTargets =
            friendlyColor == PieceColor.White ? blackTargetedSquares : whiteTargetedSquares;
        foreach (List<Vector2Int> positions in enemyTargets.Values)
        {
            if (positions.Contains(pos)) return true;
        }

        return false;
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
        List<Move> moves = new();
        List<Vector2Int> positions = new();
        foreach (Move move in piece.GetPseudolegalMoves(WorldToBoard(piece.go.transform.position), true))
        {
            if (move.isMoveLegal)
                moves.Add(move);
            positions.Add(move.endSquare);
        }
            
        if (piece.pieceData.color == PieceColor.White)
        {
            whitePseudolegalMoves.Add(piece, moves);
            whiteTargetedSquares.Add(piece, positions);
            return;
        }
        blackPseudolegalMoves.Add(piece, moves);
        blackTargetedSquares.Add(piece, positions);
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
    
    public void ShowTargetedSquares(King targetedKing)
    {
        // HideTargetedSquares();
        // foreach (List<Vector2Int> posList in targetedKing.pieceData.color == PieceColor.White ? blackTargetedSquares.Values : whiteTargetedSquares.Values)
        // {
        //     foreach (Vector2Int pos in posList)
        //     {
        //         GameObject go = Instantiate(_targetedSquareHighlighterPrefab, BoardToWorld(pos), Quaternion.identity, transform);
        //         _targetedSquaresHighlighters.Add(go);
        //     }
        // }
    }

    public void HideTargetedSquares()
    {
        // Destroy previous square highlighters
        foreach (GameObject go in _targetedSquaresHighlighters)
            Destroy(go);
        _targetedSquaresHighlighters.Clear();
    }

    public void ShowBlockOptions()
    {
        HideBlockOptions();
        foreach (Vector2Int pos in _blockCheckSquares)
        {
            GameObject go = Instantiate(_blockOptionHighlighterPrefab, BoardToWorld(pos), Quaternion.identity, transform);
            _blockOptionsHighlighters.Add(go);
        }
    }
    
    public void HideBlockOptions()
    {
        foreach (GameObject go in _blockOptionsHighlighters)
            Destroy(go);
        _blockOptionsHighlighters.Clear();
    }

    public void ShowPins()
    {
        HidePins();
        foreach (List<Vector2Int> positions in pins.Values)
        {
            foreach (Vector2Int pos in positions)
            {
                GameObject go = Instantiate(_pinHighlighterPrefab, BoardToWorld(pos), Quaternion.identity, transform);
                _pinHighlighters.Add(go);
            }
        }
    }

    public void HidePins()
    {
        foreach (GameObject go in _pinHighlighters)
            Destroy(go);
        _pinHighlighters.Clear();
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