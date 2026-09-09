using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Bishop : Piece
    {
        public Bishop(PieceData pieceData, GameObject go)
        {
            this.pieceData = pieceData;
            this.go = go;
        }
    
        public override List<Move> GetLegalMoves()
        {
            List<Move> moves = PrecomputedMoveData.GenerateSlidingMoves(this);
            return moves;
        }
    }
}