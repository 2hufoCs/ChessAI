using UnityEngine;
using System.Collections.Generic;
using Pieces;

public enum Directions { Right, Up, Left, Down, UpRight, UpLeft, DownLeft, DownRight}

public class PrecomputedMoveData
{
    public static readonly Vector2Int[] directionOffsets = 
    {
        Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down,
        new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1) 
    };
    public static readonly int[,,] numSquaresToEdges = new int[8, 8, 8];

    private static MoveGenerator _moveGenerator;

    /// <summary>
    /// Precomputes, for each square, how much distance there is with the edge of the board, in all directions.
    /// Allows for incredible performance optimization for sliding pieces (bishops/rooks/queens) instead of recalculating each time.
    /// </summary>
    public PrecomputedMoveData(MoveGenerator moveLogic)
    {
        _moveGenerator = moveLogic;
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                int numNorth = 7 - rank;
                int numSouth = rank;
                int numWest = file;
                int numEast = 7 - file;
                
                int[] squaresInfo = 
                { 
                    numEast, numNorth, numWest, numSouth,
                    Mathf.Min(numNorth, numEast), Mathf.Min(numNorth, numWest), Mathf.Min(numSouth, numWest), Mathf.Min(numSouth, numEast)
                };
                for (int i = 0; i < 8; i++)
                {
                    numSquaresToEdges[file, rank, i] = squaresInfo[i];
                }
            }
        }    
    }

    public static List<Move> GenerateSlidingMoves(Piece piece, Vector2 startPosWorld, Dictionary<Vector2, Piece> pieces)
    {
        List<Move> moves = new List<Move>();
        Vector2Int startPos = Vector2Int.RoundToInt(startPosWorld);

        int startDirIndex = piece.pieceData.type == PieceType.Bishop ? 4 : 0;
        int endDirIndex = piece.pieceData.type == PieceType.Rook ? 4 : 8;
        
        for (int directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++)
        {
            List<Move> newDirMoves = new();
            List<Vector2Int> piecePins = new();
            Vector2Int pinnedPiecePos = -Vector2Int.one;
            for (int n = 0; n < numSquaresToEdges[startPos.x, startPos.y, directionIndex]; n++)
            {
                Vector2Int targetSquare = startPos + directionOffsets[directionIndex] * (n + 1);
                int pieceOnTargetSquare = _moveGenerator.GetSquare(targetSquare);

                PieceColor friendlyColor = piece.pieceData.color;
                PieceColor enemyColor = friendlyColor == PieceColor.Black ? PieceColor.White : PieceColor.Black;
                PieceColor targetSquareColor = pieceOnTargetSquare > 8 ? PieceColor.Black : pieceOnTargetSquare != 0 ? PieceColor.White : PieceColor.None;
                PieceType targetType = pieceOnTargetSquare > 8 ? (PieceType)(pieceOnTargetSquare - 8) : (PieceType)(pieceOnTargetSquare);

                Move move = new(startPos, targetSquare);

                if (targetType == PieceType.King && targetSquareColor == enemyColor && pinnedPiecePos != -Vector2Int.one)
                {
                    piecePins.Add(startPos);
                    LegalMoveGenerator.pins[pieces[pinnedPiecePos]] = piecePins;
                    break;
                }
                
                // Blocked by friendly piece, can't move any further in that direction
                if (friendlyColor == targetSquareColor)
                    move.isMoveLegal = false;
                
                if (pinnedPiecePos == -Vector2Int.one) newDirMoves.Add(move);
                piecePins.Add(targetSquare);

                // Pin up to 1 piece, continue calculating afterwards
                if (enemyColor == targetSquareColor)
                {
                    if (pinnedPiecePos != -Vector2Int.one) break; // only a single piece can be pinned in 1 direction
                    pinnedPiecePos = targetSquare;
                }
                
                // Can't move any further in this direction after capturing opponent's piece
                if (targetSquareColor == friendlyColor)
                    break;
            }
            moves.AddRange(newDirMoves);
        }

        return moves;
    }
    
    static Vector2 WorldToBoard(Vector2 pos)
    {
        Vector2 snappedPos = new Vector2(pos.x > 0 ? (int)pos.x + 1 : (int)pos.x, pos.y > 0 ? (int)pos.y + 1 : (int)pos.y);
        return snappedPos + Vector2Int.RoundToInt(_moveGenerator.transform.position) + Vector2Int.one * 3; // Offset due to pivot point being in center of the board
    }
}