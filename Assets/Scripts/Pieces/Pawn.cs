using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Pawn : Piece
    {
        public bool disableEnPassantNextTurn;
        public bool doubleMovedLastTurn;
        
        private MoveLogic _moveLogic;
        private Dictionary<Vector2, Piece> _pieces = new();
        
        public Pawn(PieceData pieceData, GameObject go, MoveLogic moveLogic, Dictionary<Vector2, Piece> pieces)
        {
            this.pieceData = pieceData;
            this.go = go;
            _moveLogic = moveLogic;
            _pieces = pieces;
        }
    
        public override List<Move> GetPseudolegalMoves(Vector2 initialPos)
        {
            List<Move> moves = new();
            Vector2Int snappedPos = Vector2Int.RoundToInt(initialPos);
            
            // Forward step
            Vector2 forwardDir = pieceData.color == PieceColor.White ? Vector2.up : Vector2.down;
            Vector2Int forwardSquarePos = Vector2Int.RoundToInt(initialPos + forwardDir);
            CheckSingleMove(forwardSquarePos, ref moves, initialPos, false);
            
            // Double forward (if pawn hasn't moved yet)
            int startingRank = pieceData.color == PieceColor.White ? 1 : 6;
            if (snappedPos.y == startingRank)
            {
                Vector2Int doubleForwardSquarePos = Vector2Int.RoundToInt(initialPos + forwardDir * 2);
                if (CheckSingleMove(doubleForwardSquarePos, ref moves, initialPos, false))
                    doubleMovedLastTurn = true;
            }
            
            // Up-left and right-left (if there's an enemy piece)
            Vector2Int takeLeftSquarePos = Vector2Int.RoundToInt(initialPos + forwardDir + Vector2.left);
            Vector2Int takeRightSquarePos = Vector2Int.RoundToInt(initialPos + forwardDir + Vector2.right);
            CheckSingleMove(takeLeftSquarePos, ref moves, initialPos, true);
            CheckSingleMove(takeRightSquarePos, ref moves, initialPos, true);
            
            // En passant
            int enPassantRank = pieceData.color == PieceColor.White ? 4 : 3;
            if (snappedPos.y != enPassantRank) return moves;

            Pawn leftPawn = GetPawn(snappedPos + Vector2Int.left);
            Pawn rightPawn = GetPawn(snappedPos + Vector2Int.right);
            if (leftPawn == null && rightPawn == null) return moves;

            // Left en passant
            if (leftPawn != null)
            {
                if (leftPawn.doubleMovedLastTurn)
                {
                    Move newMove = new Move { startSquare = snappedPos, endSquare = snappedPos + new Vector2Int(-1, Mathf.RoundToInt(forwardDir.y)), enPassantCapture = leftPawn.go};
                    //Debug.Log("can make en passant, go to kill would be " + newMove.enPassantCapture);
                    moves.Add(newMove);
                }
            }
            
            // Right en passant
            if (rightPawn != null)
            {
                if (rightPawn.doubleMovedLastTurn)
                {
                    Move newMove = new Move { startSquare = snappedPos, endSquare = snappedPos + new Vector2Int(1, Mathf.RoundToInt(forwardDir.y)), enPassantCapture = rightPawn.go};
                    moves.Add(newMove);
                }
            }
            
            return moves;
        }

        bool CheckSingleMove(Vector2Int targetSquarePos, ref List<Move> moves, Vector2 startPos, bool haveToTake)
        {
            if (!IsInsideBounds(targetSquarePos)) return false;
            
            // Exclude move if square has friendly piece
            int targetSquare = _moveLogic.GetSquare(targetSquarePos);

            if (haveToTake)
            {
                PieceColor targetColor = targetSquare > 8 ? PieceColor.Black :
                    targetSquare > 0 ? PieceColor.White : PieceColor.None;
                if (pieceData.color == targetColor || targetColor == PieceColor.None)
                    return false;
            }
            else
            {
                if (targetSquare != 0)
                    return false;
            }


            moves.Add(new Move
            {
                startSquare = Vector2Int.RoundToInt(startPos),
                endSquare = targetSquarePos,
            });
            return true;
        }

        Pawn GetPawn(Vector2Int pos)
        {
            if (!IsInsideBounds(pos)) return null;

            int square = _moveLogic.GetSquare(pos);
            if (square is (int)PieceType.Pawn + (int)PieceColor.White or (int)PieceType.Pawn + (int)PieceColor.Black)
                return (Pawn)_pieces[pos];
            return null;
        }
        
        bool IsInsideBounds(Vector2 pos)
        {
            return pos is { x: >= 0 and < 8, y: >= 0 and < 8 };
        }
    }
}