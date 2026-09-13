using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class King : Piece
    {
        public bool isInCheck;
        
        private Dictionary<Vector2, Rook> _rooks;
        private MoveLogic _moveLogic;
        
        public bool hasMoved;

        public King(PieceData pieceData, GameObject go, MoveLogic moveLogic)
        {
            this.pieceData = pieceData;
            this.go = go;
            _moveLogic = moveLogic;
        }

        public void AssignRooks(Dictionary<Vector2, Rook> rooks)
        {
            _rooks = rooks;
        }
    
        public override List<Move> GetPseudolegalMoves(Vector2 initialPos, bool includeDefends = false)
        {
            List<Move> moves = new();
            Vector2Int snappedWholePos = Vector2Int.RoundToInt(initialPos);
            foreach (Vector2 dir in PrecomputedMoveData.directionOffsets)
            {
                Vector2Int targetSquarePos = Vector2Int.RoundToInt(initialPos + dir);
                
                // Exclude move if outside bounds
                if (!IsInsideBounds(targetSquarePos)) continue;



                Move move = new Move(snappedWholePos, targetSquarePos);
                
                // Move not legal if square is a friendly piece
                int targetSquare = _moveLogic.GetSquare(targetSquarePos);
                PieceColor targetColor = targetSquare > 8 ? PieceColor.Black : targetSquare > 0 ? PieceColor.White : PieceColor.None;
                if (pieceData.color == targetColor)
                    move.isMoveLegal = false;
                
                moves.Add(move);
            }
            
            CheckCastling(snappedWholePos, ref moves);

            return moves;
        }

        void CheckCastling(Vector2Int snappedWholePos, ref List<Move> moves)
        {
            // Castle logic
            foreach (KeyValuePair<Vector2, Rook> rook in _rooks)
            {
                if (rook.Value == null) continue;
                
                // Rule n°1: king and rooks didn't move from the beginning
                if (hasMoved || rook.Value.hasMoved) continue;
                
                Vector2Int kingSquarePos = snappedWholePos;
                Vector2Int rookSquarePos = Vector2Int.RoundToInt(rook.Key);
                int dir = kingSquarePos.x < rookSquarePos.x ? 1 : -1;
                
                bool stopCastling = false;
                for (int i = kingSquarePos.x; dir == 1 ? i < rookSquarePos.x : i > rookSquarePos.x; i += dir)
                {
                    Vector2Int pos = new Vector2Int(i, kingSquarePos.y);
                    
                    // Rule n°2: no pieces between king and rook
                    if (_moveLogic.GetSquare(pos) != 0 && i != kingSquarePos.x)
                    {
                        stopCastling = true;
                        break;
                    }
                    
                    // Rule n°3: path between king and rook can't be targeted by enemy square
                    Dictionary<Piece, List<Vector2Int>> enemyTargetedSquares = pieceData.color == PieceColor.Black ? 
                        LegalMoveLogic.Instance.whiteTargetedSquares :  LegalMoveLogic.Instance.blackTargetedSquares;
                    foreach (List<Vector2Int> pieceTargets in enemyTargetedSquares.Values)
                    {
                        if (pieceTargets.Contains(pos))
                        {
                            stopCastling = true;
                            break;
                        }
                    }

                    if (stopCastling) break;


                }
                if (stopCastling) continue;
                
                
                // If all checks have been passed, king is allowed to castle!!
                Vector2Int newKingPos = kingSquarePos + new Vector2Int(dir * 2, 0);
                Vector2Int newRookPos = newKingPos + new Vector2Int(-dir, 0);
                moves.Add(new Move
                {
                    startSquare = snappedWholePos,
                    endSquare = newKingPos,
                    isMoveLegal = true,
                    rookToCastle = rook.Value,
                    rookEndSquare = newRookPos,
                });
            }
        }
        
        bool IsInsideBounds(Vector2 pos)
        {
            return pos is { x: >= 0 and < 8, y: >= 0 and < 8 };
        }
    }
}