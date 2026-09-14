using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Rook : Piece
    {
        public bool hasMoved;
        private Dictionary<Vector2, Piece> _pieces;
        
        public Rook(PieceData pieceData, GameObject go,  Dictionary<Vector2, Piece> pieces)
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