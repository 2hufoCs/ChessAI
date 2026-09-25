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
    
    private Dictionary<Piece, List<Vector2Int>> _whiteTargetedSquares = new();
    private Dictionary<Piece, List<Vector2Int>> _blackTargetedSquares = new();
    public Dictionary<Piece, List<Vector2Int>> WhiteTargetedSquares => _whiteTargetedSquares;
    public Dictionary<Piece, List<Vector2Int>> BlackTargetedSquares => _blackTargetedSquares;
    
    private Dictionary<Piece, List<Move>> _pseudolegalMoves = new();
    private Dictionary<Piece, List<Move>> _legalMoves = new();
    public Dictionary<Piece, List<Move>> LegalMoves => _legalMoves;
    private Dictionary<GameObject, Piece> _goToPieces = new();
    public Dictionary<GameObject, Piece> GoToPieces => _goToPieces;
    
    // Used to deal with checks
    private bool _canBlockCheck;
    public  bool CanBlockCheck => _canBlockCheck;
    private int _enemyCheckCount;
    public int EnemyCheckCount => _enemyCheckCount;
    private bool _canTakeChecker;
    
    private List<Vector2Int> _blockCheckSquares = new();
    public  List<Vector2Int> BlockCheckSquares => _blockCheckSquares;
    private List<Vector2Int> _lookThroughKingSquares = new();
    public List<Vector2Int> LookThroughKingSquares => _lookThroughKingSquares;
    
    // Used to deal with pins
    public Dictionary<Piece, List<Vector2Int>> pins = new();
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
    
    // All following fields are performance-related
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
        _legalMoves = new();
        
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
                _legalMoves[pieceMoves.Key] = movesWithoutPins;
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
            if (newMoves.Count > 0) _legalMoves.Add(pieceMoves.Key, newMoves);
        }
        
        trulyLegalCount +=  (DateTime.Now - initialTime).Milliseconds;

        return _legalMoves;
    }
    
    public Dictionary<Piece, List<Move>> GetPseudoLegalMoves(Dictionary<Vector2Int, Piece> pieces, King targetKing)
    {
        //Debug.Log("ye");
        // Reinitialize lists
        _pseudolegalMoves.Clear();
        _whiteTargetedSquares.Clear();
        _blackTargetedSquares.Clear();
        
        // Reset a bunch of stuff
        _enemyCheckCount = 0;
        targetKing.isInCheck = false;
        _canBlockCheck = true;
        _canTakeChecker = true;
        
        _blockCheckSquares.Clear();
        _lookThroughKingSquares.Clear();
        
        // Reset defenders data
        foreach (KeyValuePair<Vector2Int, Piece> piece in pieces)
            piece.Value.isDefended = false;

        List<KeyValuePair<Vector2Int, Piece>> piecesToLookForCheck = new();
        foreach (KeyValuePair<Vector2Int, Piece> piece in pieces)
        {
            if (piece.Value.pieceData.color == targetKing.pieceData.color)
            {
                piecesToLookForCheck.Add(piece);
                continue;
            }
            
            // Get piece pseudo legal moves, track time taken
            DateTime initialTime = DateTime.Now;
            
            List<Move> pseudoMoves = piece.Value.GetPseudolegalMoves(piece.Key);
            if (pseudoMoves.Count == 0) continue;
            
            _pseudolegalMoves.Add(piece.Value, new List<Move>());
            _pseudolegalMoves[piece.Value].AddRange(pseudoMoves);
            
            int newTime = (DateTime.Now - initialTime).Milliseconds;
            piecePseudoLegalCount += newTime;
            StorePiecePerf(piece.Value, newTime);
        }
        
        foreach (KeyValuePair<Vector2Int, Piece> piece in  piecesToLookForCheck)
            LookForCheck(piece, targetKing == whiteKing ? blackKing : whiteKing, pieces);
        
        if (BoardSettings.Instance.debugLegalMoves)
        {
            ShowTargetedSquares(targetKing);
            ShowBlockOptions();
            ShowPins();
        }

        return _pseudolegalMoves;
    }

    void LookForCheck(KeyValuePair<Vector2Int, Piece> piece, King targetKing, Dictionary<Vector2Int, Piece> pieces)
    {
        Debug.Log("looking for check with " + piece.Value);
        Vector2Int kingPos = Vector2Int.RoundToInt(WorldToBoard(targetKing.go.transform.position));
        List<Vector2Int> targetedSquares = new();

        foreach (Move move in piece.Value.GetPseudolegalMoves(piece.Key))
        {
            targetedSquares.Add(move.endSquare);
            if (move.endSquare == kingPos)
                GetKingLinesOfAttack(targetKing, piece);
        }

        if (piece.Value.isDefended)
        {
            targetedSquares.Add(piece.Key);
            if (piece.Value.pieceData.type == PieceType.Queen) Debug.Log("defended");
        }
        
        if (targetKing.pieceData.color == PieceColor.White)
            _blackTargetedSquares.Add(piece.Value, targetedSquares);
        else _whiteTargetedSquares.Add(piece.Value, targetedSquares);
    }

    void GetKingLinesOfAttack(King targetKing, KeyValuePair<Vector2Int, Piece> piece)
    {
        Debug.Log("check, man");
        // King is in check, verify if player can block or not
        targetKing.isInCheck = true;
        _enemyCheckCount++;
        _canBlockCheck &= _enemyCheckCount < 2;
        _canTakeChecker &= _enemyCheckCount < 2;
        
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

    public void LoadBoardState(BoardState state)
    {
        _legalMoves = state.LegalMovesStates;
        _goToPieces = state.GoToPiecesStates;
        _whiteTargetedSquares = state.WhiteTargetedSquares;
        _blackTargetedSquares = state.BlackTargetedSquares;
        _enemyCheckCount = state.EnemyCheckCount;
        _blockCheckSquares = state.BlockCheckSquares;
        _canBlockCheck = state.CanBlockCheck;
        _lookThroughKingSquares = state.LookThroughKingSquares;
        pins =  state.Pins;
        whiteKing = state.WhiteKing;
        blackKing = state.BlackKing;
    }
    
    public Piece GetPieceFromGO(GameObject go)
    {
        foreach (Piece piece in _legalMoves.Keys)
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
            friendlyColor == PieceColor.White ? _blackTargetedSquares : _whiteTargetedSquares;
        foreach (List<Vector2Int> positions in enemyTargets.Values)
        {
            if (positions.Contains(pos)) return true;
        }

        Debug.Log($"for {friendlyColor} king, pos {pos} isn't targeted by enemy");
        return false;
    }

    public void RemovePieceFromLegalMoves(Piece piece)
    {
        if (piece.pieceData.color == PieceColor.White)
        {
            _pseudolegalMoves.Remove(piece);
            _whiteTargetedSquares.Remove(piece);
            return;
        }
        _pseudolegalMoves.Remove(piece);
        _blackTargetedSquares.Remove(piece);
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
            _pseudolegalMoves.Add(piece, moves);
            _whiteTargetedSquares.Add(piece, positions);
            return;
        }
        _pseudolegalMoves.Add(piece, moves);
        _blackTargetedSquares.Add(piece, positions);
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
        // foreach (List<Vector2Int> posList in targetedKing.pieceData.color == PieceColor.White ? _blackTargetedSquares.Values : _whiteTargetedSquares.Values)
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