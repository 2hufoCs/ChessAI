using System;
using Pieces;
using UnityEngine;

public struct Move : IEquatable<Move>
{
    public Vector2Int startSquare;
    public Vector2Int endSquare;
    public Piece preMovePieceCopy;

    // For different ways to draw
    public DrawOutcomes drawOutcome;
    
    public Piece pieceOnTargetSquare;
    public Piece preMoveTargetPiece;
    
    // Pawns-specific stuff
    public Piece enPassantCapture;
    public Piece preMoveEnPassantCapture;
    
    // For castling
    public Rook rookToCastle;
    public Rook preMoveRookCastle;
    public Vector2Int rookStartSquare;
    public Vector2Int rookEndSquare;

    public Move(Vector2Int startSquare, Vector2Int endSquare, Piece preMovePieceCopy)
    {
        this.startSquare = startSquare;
        this.endSquare = endSquare;
        this.preMovePieceCopy = preMovePieceCopy;
        pieceOnTargetSquare = null;
        
        drawOutcome = DrawOutcomes.None;
        
        preMoveTargetPiece = null;
        preMoveEnPassantCapture = null;
        preMoveRookCastle = null;

        enPassantCapture = null;
        rookToCastle = null;
        rookStartSquare = -Vector2Int.one;
        rookEndSquare = -Vector2Int.one;
    }

    public bool Equals(Move other)
    {
        return startSquare.Equals(other.startSquare) && endSquare.Equals(other.endSquare);
    }

    public override bool Equals(object obj)
    {
        return obj is Move other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(startSquare, endSquare);
    }
}