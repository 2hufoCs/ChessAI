using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Queen : Piece
    {
        public Dictionary<Vector2Int, Piece> _pieces;

        public Queen(PieceData pieceData, GameObject go, Dictionary<Vector2Int, Piece> pieces)
        {
            this.pieceData = pieceData;
            this.go = go;
            _pieces = pieces;
        }

        public override Piece DeepCopy(Piece pieceToCopy, Piece pieceToOverwrite = null)
        {
            Queen q1 = (Queen)pieceToCopy;
            
            if (pieceToOverwrite == null)
                return new Queen(q1.pieceData, q1.go, q1._pieces);
            
            Queen q2 = (Queen)pieceToOverwrite;
            q2.pieceData = q1.pieceData;
            q2.go = q1.go;
            q2._pieces = q1._pieces;
            return q2;
        }

        public override List<Move> GetPseudolegalMoves(Vector2 initialPos, bool includeDefends = false)
        {
            List<Move> moves = PrecomputedMoveData.GenerateSlidingMoves(this, initialPos, _pieces);
            return moves;
        }
    }
}