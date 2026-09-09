using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Pawn : Piece
    {
        public Pawn(PieceData pieceData, GameObject go)
        {
            this.pieceData = pieceData;
            this.go = go;
        }
    
        public override List<Move> GetLegalMoves()
        {
            List<Move> moves = new();

            return moves;
        }
    }
}