using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Bishop : Piece
    {
        MoveLogic _moveLogic;
        
        public Bishop(PieceData pieceData, GameObject go, MoveLogic moveLogic)
        {
            this.pieceData = pieceData;
            this.go = go;
            _moveLogic = moveLogic;
        }

        public override Piece DeepCopy(Piece pieceToCopy, Piece pieceToOverwrite = null)
        {
            Bishop b1 =  (Bishop)pieceToCopy;
            
            if (pieceToOverwrite == null)
                return new Bishop(b1.pieceData, b1.go, b1._moveLogic);
            
            Bishop b2 = (Bishop)pieceToOverwrite;
            b2.pieceData = b1.pieceData;
            b2.go = b1.go;
            b2._moveLogic = b1._moveLogic;
            return b2;
        }

        public override List<Move> GetPseudolegalMoves(Vector2 initialPos)
        {
            List<Move> moves = PrecomputedMoveData.GenerateSlidingMoves(this, initialPos, _moveLogic._pieces);
            return moves;
        }
    }
}