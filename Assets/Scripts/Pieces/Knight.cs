using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Knight : Piece
    {
        public Knight(PieceData pieceData, GameObject go)
        {
            this.pieceData = pieceData;
            this.go = go;
        }
    
        public override List<Move> GetLegalMoves(Vector2 initialPos)
        {
            List<Move> moves = new();

            return moves;
        }
    }
}