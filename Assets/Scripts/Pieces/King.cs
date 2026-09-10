using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class King : Piece
    {
        private MoveLogic _moveLogic;
        
        public King(PieceData pieceData, GameObject go, MoveLogic moveLogic)
        {
            this.pieceData = pieceData;
            this.go = go;
            _moveLogic = moveLogic;
        }
    
        public override List<Move> GetLegalMoves(Vector2 initialPos)
        {
            List<Move> moves = new();
            foreach (Vector2 dir in PrecomputedMoveData.directionOffsets)
            {
                Vector2Int targetSquarePos = Vector2Int.RoundToInt(initialPos + dir);
                
                // Exclude move if outside bounds
                if (!IsInsideBounds(targetSquarePos)) continue;
                
                // Exclude move if square has friendly piece
                int targetSquare = _moveLogic.GetSquare(targetSquarePos);
                PieceColor targetColor = targetSquare > 8 ? PieceColor.Black :
                    targetSquare > 0 ? PieceColor.White : PieceColor.None;
                if (pieceData.color == targetColor)
                    continue;
                
                moves.Add(new Move
                {
                    startSquare = Vector2Int.RoundToInt(initialPos),
                    endSquare = targetSquarePos,
                });
            }

            return moves;
        }
        
        bool IsInsideBounds(Vector2 pos)
        {
            return pos is { x: >= 0 and < 8, y: >= 0 and < 8 };
        }
    }
}