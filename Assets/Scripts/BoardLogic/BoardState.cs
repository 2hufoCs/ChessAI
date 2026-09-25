using System.Collections.Generic;
using System.Linq;
using Pieces;
using UnityEngine;

public class BoardState
{
    public static Stack<BoardState> boardStates = new();

    private MoveLogic _moveLogic;
    private LegalMoveLogic _legalLogic;

    public int[,] BoardValueStates { get; } = new int[8, 8];
    public Dictionary<Vector2Int, Piece> PiecesStates { get; } = new();
    public Dictionary<Piece, List<Move>> LegalMovesStates { get; } = new();
    public Dictionary<GameObject, Piece> GoToPiecesStates { get; } = new();
    public PieceColor PlayerColor { get; }

    // Check and pins stuff
    public Dictionary<Piece, List<Vector2Int>> WhiteTargetedSquares { get; } = new();
    public Dictionary<Piece, List<Vector2Int>> BlackTargetedSquares { get; } = new();
    public int EnemyCheckCount { get; }
    public List<Vector2Int> BlockCheckSquares { get; } = new();
    public List<Vector2Int> LookThroughKingSquares { get; } = new();
    public Dictionary<Piece, List<Vector2Int>> Pins { get; } = new();
    
    public King WhiteKing { get; }
    public King BlackKing { get; }

    // Used for promotion
    private Piece pawnBeforePromotion;
    private Piece pawnJustPromoted;
    private Sprite pawnSprite;

    public BoardState(MoveLogic moveLogic, LegalMoveLogic legalLogic)
    {
        _moveLogic = moveLogic;
        _legalLogic = legalLogic;
        
        // Board values (represented as ints)
        BoardValueStates = moveLogic.Board.Clone() as int[,];
        
        // Pieces
        Dictionary<Vector2Int, Piece> piecesCopy = new();
        foreach (KeyValuePair<Vector2Int, Piece> piece in moveLogic.Pieces)
        {
            Piece copy = piece.Value.DeepCopy(piece.Value);
            piecesCopy[piece.Key] = copy;
        }
        PiecesStates = piecesCopy;
        
        // Legal moves (so the ai doesn't recompute them when unmaking a move)
        LegalMovesStates = legalLogic.LegalMoves.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        // Gameobjects to pieces, for easy access
        GoToPiecesStates = legalLogic.GoToPieces.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        PlayerColor = moveLogic.ColorToPlay;
        
        // Additional check data
        WhiteTargetedSquares = legalLogic.WhiteTargetedSquares.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        BlackTargetedSquares = legalLogic.BlackTargetedSquares.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        EnemyCheckCount = legalLogic.EnemyCheckCount;
        BlockCheckSquares = legalLogic.BlockCheckSquares;
        LookThroughKingSquares = legalLogic.LookThroughKingSquares;
        Pins = legalLogic.pins.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        WhiteKing = (King)_legalLogic.whiteKing.DeepCopy(_legalLogic.whiteKing);
        BlackKing = (King)_legalLogic.blackKing.DeepCopy(_legalLogic.blackKing);

        pawnBeforePromotion = null;
        pawnJustPromoted = null;
        
        boardStates.Push(this);
    }

    public void LoadBoardState()
    {
        _moveLogic.LoadBoardState(this);
        _legalLogic.LoadBoardState(this);
    }

    public static void UndoBoardState()
    {
        BoardState stateToRemove = boardStates.Pop();

        if (stateToRemove.pawnBeforePromotion != null && stateToRemove.pawnJustPromoted != null)
        {
            // Overwrite promoted piece with old pawn
            stateToRemove.pawnJustPromoted.go =  stateToRemove.pawnBeforePromotion.go;
            stateToRemove.pawnJustPromoted = stateToRemove.pawnBeforePromotion;
            stateToRemove.pawnJustPromoted.go.GetComponent<SpriteRenderer>().sprite = stateToRemove.pawnSprite;
        }
        Debug.Log("undid board state, count is now: " + boardStates.Count);
    }

    public void SetPawnBeforePromotion(Pawn pawn)
    {
        pawnBeforePromotion = (Pawn)pawn.DeepCopy(pawn);
        pawnSprite = pawn.pieceData.sprite;
    }

    public void SetPromotedPiece(Piece piece)
    {
        pawnJustPromoted = piece.DeepCopy(piece);
    } 
    
    Vector2Int WorldToBoard(Vector2 pos)
    {
        Vector2 snappedPos = new Vector2(pos.x > 0 ? (int)pos.x + 1 : (int)pos.x, pos.y > 0 ? (int)pos.y + 1 : (int)pos.y);
        return Vector2Int.RoundToInt(snappedPos + Vector2.one * 3);
    }
}
