using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Queen : Piece
    {
        MoveLogic _moveLogic;

        public Queen(PieceData pieceData, GameObject go, MoveLogic moveLogic)
        {
            this.pieceData = pieceData;
            this.go = go;
            _moveLogic = moveLogic;
        }

        public override Piece DeepCopy(Piece pieceToCopy, Piece pieceToOverwrite = null)
        {
            Queen q1 = (Queen)pieceToCopy;
            
            if (pieceToOverwrite == null)
                return new Queen(q1.pieceData, q1.go, q1._moveLogic);
            
            Queen q2 = (Queen)pieceToOverwrite;
            q2.pieceData = q1.pieceData;
            q2.go = q1.go;
            q2._moveLogic = q1._moveLogic;
            return q2;
        }

        public override List<Move> GetPseudolegalMoves(Vector2 initialPos)
        {
            List<Move> moves = PrecomputedMoveData.GenerateSlidingMoves(this, initialPos, _moveLogic._pieces);
            return moves;
        }
    }
}