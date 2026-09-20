using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Rook : Piece
    {
        public bool hasMoved;
        public Dictionary<Vector2Int, Piece> _pieces;
        
        public Rook(PieceData pieceData, GameObject go,  Dictionary<Vector2Int, Piece> pieces)
        {
            this.pieceData = pieceData;
            this.go = go;
            _pieces = pieces;
        }

        public override Piece DeepCopy(Piece pieceToCopy, Piece pieceToOverwrite = null)
        {
            Rook r1 =  (Rook)pieceToCopy;

            if (pieceToOverwrite == null)
            {
                Rook rook = new(r1.pieceData, r1.go, r1._pieces)
                {
                    hasMoved = r1.hasMoved
                };
                return rook;
            }

            Rook r2 = (Rook)pieceToOverwrite;
            r2.pieceData = r1.pieceData;
            r2.go = r1.go;
            r2._pieces = r1._pieces;
            r2.hasMoved = r1.hasMoved;
            return r2;
        }

        public override List<Move> GetPseudolegalMoves(Vector2 initialPos, bool includeDefends = false)
        {
            List<Move> moves = PrecomputedMoveData.GenerateSlidingMoves(this, initialPos, _pieces);
            return moves;
        }
    }
}