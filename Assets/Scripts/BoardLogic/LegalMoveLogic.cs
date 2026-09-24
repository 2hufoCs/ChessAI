using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NaughtyAttributes;
using NUnit.Framework.Constraints;
using Pieces;
using UnityEngine;

public enum DrawOutcomes { None, Stalemate, ThreefoldRepetition, InsufficientMaterial, FiftyMoveRule }

public class LegalMoveLogic : MonoBehaviour
{
    public static LegalMoveLogic Instance { get;  private set; }
    
    public Dictionary<Piece, List<Vector2Int>> whiteTargetedSquares = new();
    public Dictionary<Piece, List<Vector2Int>> blackTargetedSquares = new();
    
    Dictionary<Piece, List<Move>> pseudolegalMoves = new();
    public Dictionary<Piece, List<Move>> legalMoves = new();
    public Dictionary<GameObject, Piece> goToPieces = new();
    
    // Used to deal with checks
    private bool _canBlockCheck;
    private bool _canTakeChecker;
    private int enemyCheckCount;
    private List<Vector2Int> _blockCheckSquares = new();
    private List<Vector2Int> _lookThroughKingSquares = new();
    
    // Used to deal with pins
    public static Dictionary<Piece, List<Vector2Int>> pins = new();
    public King whiteKing;
    public King blackKing;
    
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

    private int pseudoLegalCount = 0;
    private int trulyLegalCount = 0;
    
    private int piecePseudoLegalCount = 0;
    private int pawnPseudoLegalCount = 0;
    private int knightPseudoLegalCount = 0;
    private int bishopPseudoLegalCount = 0;
    private int rookPseudoLegalCount = 0;
    private int queenPseudoLegalCount = 0;
    private int kingPseudoLegalCount = 0;

    private int sequentialForeachCount = 0;
    private int multithreadForeachCount = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(Instance);
        Instance = this;
    }

    public Dictionary<Piece, List<Move>> GetAllLegalMoves(Dictionary<Vector2Int, Piece> pieces, PieceColor colorToPlay)
    {
        King targetKing = colorToPlay ==  PieceColor.Black ? whiteKing : blackKing;
        King friendlyKing = colorToPlay == PieceColor.Black ? blackKing : whiteKing;
        
        pins = new();
        DateTime initialTime = DateTime.Now;
        Dictionary<Piece, List<Move>> pseudoLegalMoves = GetPseudoLegalMoves(pieces, targetKing);
        pseudoLegalCount += (DateTime.Now - initialTime).Milliseconds;
        legalMoves = new();
        
        initialTime = DateTime.Now;
        
        // Iterate through every piece that has pseudo legal moves
        foreach (KeyValuePair<Piece, List<Move>> pieceMoves in pseudoLegalMoves)
        {
            // For pieces that are pinned
            List<Move> movesWithoutPins = new();
            
            if (pins.TryGetValue(pieceMoves.Key, out List<Vector2Int> posToRemove))
            {
                // Piece is located in pins dict, only allow moves inside list
                foreach (Move move in pieceMoves.Value)
                {
                    if (pins[pieceMoves.Key].Contains(move.endSquare))
                        movesWithoutPins.Add(move);
                }
            }
            else movesWithoutPins = new(pieceMoves.Value);
            
            if (!friendlyKing.isInCheck && pieceMoves.Key != friendlyKing)
            {
                legalMoves[pieceMoves.Key] = movesWithoutPins;
                continue;
            }
            
            List<Move> newMoves = new();
            foreach (Move move in movesWithoutPins)
            {
                // If piece is king, simply need to move to non-targeted square
                if (pieceMoves.Key == friendlyKing)
                {
                    if (!IsSquareTargeted(move.endSquare, friendlyKing.pieceData.color) && !_lookThroughKingSquares.Contains(move.endSquare))
                        newMoves.Add(move);
                    continue;
                }

                // Otherwise, only allow capture of the attacker
                if (!_canBlockCheck || !friendlyKing.isInCheck)
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
        
        trulyLegalCount +=  (DateTime.Now - initialTime).Milliseconds;

        return legalMoves;
    }
    
    public Dictionary<Piece, List<Move>> GetPseudoLegalMoves(Dictionary<Vector2Int, Piece> pieces, King targetKing)
    {
        // Reinitialize lists
        Dictionary<Piece, List<Vector2Int>> targetedSquares = new();
        pseudolegalMoves.Clear();
        whiteTargetedSquares.Clear();
        blackTargetedSquares.Clear();
        
        // Reset a bunch of stuff
        enemyCheckCount = 0;
        targetKing.isInCheck = false;
        _canBlockCheck = true;
        _canTakeChecker = true;
        
        _blockCheckSquares.Clear();
        _lookThroughKingSquares.Clear();
        
        // Reset defenders data
        foreach (KeyValuePair<Vector2Int, Piece> piece in pieces)
            piece.Value.isDefended = false;
        
        foreach (KeyValuePair<Vector2Int, Piece> piece in pieces)
        {
            if (piece.Value.pieceData.color == targetKing.pieceData.color)
            {
                LookForCheck(piece, targetKing == whiteKing ? blackKing : whiteKing, pieces);
                continue;
            }
            
            // Get piece pseudo legal moves, track time taken
            DateTime initialTime = DateTime.Now;
            
            List<Move> pseudoMoves = piece.Value.GetPseudolegalMoves(piece.Key);
            pseudolegalMoves.Add(piece.Value, new List<Move>());
            pseudolegalMoves[piece.Value].AddRange(pseudoMoves);
            
            int newTime = (DateTime.Now - initialTime).Milliseconds;
            piecePseudoLegalCount += newTime;
            StorePiecePerf(piece.Value, newTime);
        }
        
        if (BoardSettings.Instance.debugLegalMoves)
        {
            ShowTargetedSquares(targetKing);
            ShowBlockOptions();
            ShowPins();
        }
        return pseudolegalMoves;
    }

    void LookForCheck(KeyValuePair<Vector2Int, Piece> piece, King targetKing, Dictionary<Vector2Int, Piece> pieces)
    {
        Vector2Int kingPos = Vector2Int.RoundToInt(WorldToBoard(targetKing.go.transform.position));
        List<Vector2Int> targetedSquares = new();

        foreach (Move move in piece.Value.GetPseudolegalMoves(piece.Key))
        {
            targetedSquares.Add(move.endSquare);
            if (move.endSquare == kingPos)
            {
                // King is in check, verify if player can block or not
                targetKing.isInCheck = true;
                enemyCheckCount++;
                _canBlockCheck &= enemyCheckCount < 2;
                _canTakeChecker &= enemyCheckCount < 2;
            
                GetKingLinesOfAttack(targetKing, piece);
            }
        }
        if (piece.Value.isDefended)
            targetedSquares.Add(piece.Key);
        
        if (targetKing.pieceData.color == PieceColor.White)
            blackTargetedSquares.Add(piece.Value, targetedSquares);
        else whiteTargetedSquares.Add(piece.Value, targetedSquares);
    }

    void GetKingLinesOfAttack(King targetKing, KeyValuePair<Vector2Int, Piece> piece)
    {
        Vector2Int basePos = Vector2Int.RoundToInt(piece.Key);
        Vector2Int kingPos = Vector2Int.RoundToInt(WorldToBoard(targetKing.go.transform.position));
        _blockCheckSquares.Add(basePos);
            
        PieceType enemyType = piece.Value.pieceData.type;
        if (enemyType != PieceType.Bishop && enemyType != PieceType.Rook && enemyType != PieceType.Queen)
        {
            _canBlockCheck = false;
            return;
        }
            
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

    public Piece GetPieceFromGO(GameObject go)
    {
        foreach (Piece piece in legalMoves.Keys)
        {
            if (piece.go == go) return piece;
        }

        return null;
    }

    void StorePiecePerf(Piece piece, int time)
    {
        switch (piece.pieceData.type)
        {
            case PieceType.Pawn:
                pawnPseudoLegalCount += time;
                break;
            case PieceType.Knight:
                knightPseudoLegalCount += time;
                break;
            case PieceType.Bishop:
                bishopPseudoLegalCount += time;
                break;
            case PieceType.Queen:
                queenPseudoLegalCount += time;
                break;
            case PieceType.Rook:
                rookPseudoLegalCount += time;
                break;
            case PieceType.King:
                kingPseudoLegalCount += time;
                break;
        }
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
            pseudolegalMoves.Remove(piece);
            whiteTargetedSquares.Remove(piece);
            return;
        }
        pseudolegalMoves.Remove(piece);
        blackTargetedSquares.Remove(piece);
    }

    public void AddPieceToLegalMoves(Piece piece)
    {
        List<Move> moves = new();
        List<Vector2Int> positions = new();
        Vector2Int piecePos = WorldToBoard(piece.go.transform.position);
        foreach (Move move in piece.GetPseudolegalMoves(piecePos))
        {
            moves.Add(move);
            positions.Add(move.endSquare);
        }
        if (piece.isDefended) positions.Add(piecePos);
            
        if (piece.pieceData.color == PieceColor.White)
        {
            pseudolegalMoves.Add(piece, moves);
            whiteTargetedSquares.Add(piece, positions);
            return;
        }
        pseudolegalMoves.Add(piece, moves);
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

    
    public void GetLegalMovePerfInfo()
    {
        Debug.Log("pseudo legal count: " + pseudoLegalCount);
        Debug.Log("truly legal count: " + trulyLegalCount);
        Debug.Log("piece pseudo legal count:  " + piecePseudoLegalCount);
        
        Debug.Log("pawns: " + pawnPseudoLegalCount);
        Debug.Log("knights: " + knightPseudoLegalCount);
        Debug.Log("bishops: " + bishopPseudoLegalCount);
        Debug.Log("queens: " + queenPseudoLegalCount);
        Debug.Log("rooks: " + rookPseudoLegalCount);
        Debug.Log("kings: " + kingPseudoLegalCount);
        
        Debug.Log("sequential foreach: " + sequentialForeachCount);
        Debug.Log("multithreading foreach: " + multithreadForeachCount);
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
    
    Vector2Int WorldToBoard(Vector2 pos)
    {
        Vector2 snappedPos = new Vector2(pos.x > 0 ? (int)pos.x + 1 : (int)pos.x, pos.y > 0 ? (int)pos.y + 1 : (int)pos.y);
        return Vector2Int.RoundToInt(snappedPos + (Vector2)transform.position + Vector2.one * 3);
    }
}