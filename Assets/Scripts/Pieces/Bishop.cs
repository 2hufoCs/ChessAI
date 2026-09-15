using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Bishop : Piece
    {
        Dictionary<Vector2, Piece> _pieces;
        
        public Bishop(PieceData pieceData, GameObject go, Dictionary<Vector2, Piece> pieces)
        {
            this.pieceData = pieceData;
            this.go = go;
            _pieces = pieces;
        }

        public override Piece DeepCopy(Piece pieceToCopy, Piece pieceToOverwrite = null)
        {
            Bishop b1 =  (Bishop)pieceToCopy;
            
            if (pieceToOverwrite == null)
                return new Bishop(b1.pieceData, b1.go, b1._pieces);
            
            Bishop b2 = (Bishop)pieceToOverwrite;
            b2.pieceData = b1.pieceData;
            b2.go = b1.go;
            b2._pieces = _pieces;
            return b2;
        }

        public override List<Move> GetPseudolegalMoves(Vector2 initialPos, bool includeDefends = false)
        {
            List<Move> moves = PrecomputedMoveData.GenerateSlidingMoves(this, initialPos, _pieces);
            return moves;
        }
    }
}