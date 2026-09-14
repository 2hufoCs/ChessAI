using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Queen : Piece
    {
        private Dictionary<Vector2, Piece> _pieces;

        public Queen(PieceData pieceData, GameObject go, Dictionary<Vector2, Piece> pieces)
        {
            this.pieceData = pieceData;
            this.go = go;
            _pieces = pieces;
        }
    
        public override List<Move> GetPseudolegalMoves(Vector2 initialPos, bool includeDefends = false)
        {
            List<Move> moves = PrecomputedMoveData.GenerateSlidingMoves(this, initialPos, _pieces);
            return moves;
        }
    }
}