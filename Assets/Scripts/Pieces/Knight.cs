using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Knight : Piece
    {
        private readonly Vector2[] _knightDirections =
        {
            new Vector2(1, 2), new Vector2(2, 1),  new Vector2(2, -1),   new Vector2(1, -2),
            new Vector2(-1, -2), new Vector2(-2, -1), new Vector2(-2, 1),  new Vector2(-1, 2)
        };
        private MoveLogic _moveLogic;
        
        public Knight(PieceData pieceData, GameObject go,  MoveLogic moveLogic)
        {
            this.pieceData = pieceData;
            this.go = go;
            _moveLogic = moveLogic;
        }
    
        public override List<Move> GetPseudolegalMoves(Vector2 initialPos, bool includeDefends = false)
        {
            List<Move> moves = new();
            
            // Iterate over knight moves
            for (int i = 0; i < _knightDirections.Length; i++)
            {
                Vector2Int targetSquarePos = Vector2Int.RoundToInt(initialPos + _knightDirections[i]);
                
                // Exclude move if outside bounds
                if (!IsInsideBounds(targetSquarePos)) continue;
                
                Move move = new Move(Vector2Int.RoundToInt(initialPos), targetSquarePos);
                
                // Exclude move if square has friendly piece
                int targetSquare = _moveLogic.GetSquare(targetSquarePos);
                PieceColor targetColor = targetSquare > 8 ? PieceColor.Black :
                    targetSquare > 0 ? PieceColor.White : PieceColor.None;
                if (pieceData.color == targetColor)
                    move.isMoveLegal = false;
                
                moves.Add(move);
            }

            return moves;
        }
        
        bool IsInsideBounds(Vector2 pos)
        {
            return pos is { x: >= 0 and < 8, y: >= 0 and < 8 };
        }
    }
}