using System.Collections.Generic;
using System.Linq;
using Pieces;
using Unity.VisualScripting;
using UnityEngine;

public class BoardState
{
    public static Stack<BoardState> boardStates = new();
    
    public readonly int[,] boardValueStates = new int[8, 8];
    public readonly Dictionary<Vector2Int, Piece> piecesStates = new();
    public readonly Dictionary<Piece, List<Move>> legalMovesStates = new();
    public readonly Dictionary<GameObject, Piece> goToPiecesStates = new();

    public readonly PieceColor playerColor;

    // Used for promotion
    public Piece pawnBeforePromotion;
    public Piece pawnJustPromoted;
    public Sprite pawnSprite;

    public BoardState(int[,] boardValueStates, Dictionary<Vector2Int, Piece> piecesStates, Dictionary<Piece, List<Move>> legalMovesStates, 
        Dictionary<GameObject, Piece> goToPiecesStates, PieceColor playerColor)
    {
        // Board values (represented as ints)
        this.boardValueStates = boardValueStates.Clone() as int[,];
        
        // Pieces
        Dictionary<Vector2Int, Piece> piecesCopy = new();
        foreach (KeyValuePair<Vector2Int, Piece> piece in piecesStates)
        {
            Piece copy = piece.Value.DeepCopy(piece.Value);
            piecesCopy[piece.Key] = copy;
        }
        this.piecesStates = piecesCopy;
        
        // Legal moves (so the ai doesn't recompute them when unmaking a move)
        this.legalMovesStates = legalMovesStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        // Gameobjects to pieces, for easy access
        this.goToPiecesStates = goToPiecesStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        this.playerColor = playerColor;

        pawnBeforePromotion = null;
        pawnJustPromoted = null;
        
        boardStates.Push(this);
    }

    public void LoadBoardState(ref int[,] board, ref Dictionary<Vector2Int, Piece> pieces, 
        ref Dictionary<Piece, List<Move>> legalMoves, ref Dictionary<GameObject, Piece> goToPieces, ref PieceColor colorToPlay)
    {
        board = boardValueStates.Clone() as int[,];
        
        Dictionary<Vector2Int, Piece> piecesCopy = new();
        foreach (KeyValuePair<Vector2Int, Piece> piece in piecesStates)
        {
            Piece copy = piece.Value.DeepCopy(piece.Value);
            piecesCopy[piece.Key] = copy;
        }
        pieces =  piecesCopy;
        
        legalMoves = legalMovesStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        goToPieces = goToPiecesStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        colorToPlay = playerColor;
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
