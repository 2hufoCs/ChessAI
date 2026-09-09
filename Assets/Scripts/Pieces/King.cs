using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class King : Piece
    {
        public King(PieceData pieceData, GameObject go)
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