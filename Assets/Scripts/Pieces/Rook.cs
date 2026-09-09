using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Rook : Piece
    {
        public Rook(PieceData pieceData, GameObject go)
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