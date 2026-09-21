using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Piece
{
    public PieceData pieceData;
    public GameObject go;
    public bool isDefended;

    public abstract List<Move> GetPseudolegalMoves(Vector2 initialPos);
    
    public abstract Piece DeepCopy(Piece pieceToCopy, Piece pieceToOverwrite = null);
}