using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Rook : Piece
    {
        public bool hasMoved;
        
        public Rook(PieceData pieceData, GameObject go)
        {
            this.pieceData = pieceData;
            this.go = go;
        }
    
        public override List<Move> GetPseudolegalMoves(Vector2 initialPos)
        {
            List<Move> moves = PrecomputedMoveData.GenerateSlidingMoves(this, initialPos);
            return moves;
        }
    }
}