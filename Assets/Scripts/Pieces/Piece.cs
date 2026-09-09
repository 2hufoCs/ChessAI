using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Piece
{
    public PieceData pieceData;
    public GameObject go;

    public abstract List<Move> GetLegalMoves();
}


