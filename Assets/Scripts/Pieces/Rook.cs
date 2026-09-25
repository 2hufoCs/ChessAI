using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Rook : Piece
    {
        public bool hasMoved;
        private MoveLogic _moveLogic;
        
        public Rook(PieceData pieceData, GameObject go,  MoveLogic moveLogic)
        {
            this.pieceData = pieceData;
            this.go = go;
            _moveLogic = moveLogic;
        }

        public override Piece DeepCopy(Piece pieceToCopy, Piece pieceToOverwrite = null)
        {
            Rook r1 =  (Rook)pieceToCopy;

            if (pieceToOverwrite == null)
            {
                Rook rook = new(r1.pieceData, r1.go, r1._moveLogic)
                {
                    hasMoved = r1.hasMoved
                };
                return rook;
            }

            Rook r2 = (Rook)pieceToOverwrite;
            r2.pieceData = r1.pieceData;
            r2.go = r1.go;
            r2._moveLogic = r1._moveLogic;
            r2.hasMoved = r1.hasMoved;
            return r2;
        }

        public override List<Move> GetPseudolegalMoves(Vector2 initialPos)
        {
            List<Move> moves = PrecomputedMoveData.GenerateSlidingMoves(this, initialPos, _moveLogic.Pieces);
            return moves;
        }
    }
}