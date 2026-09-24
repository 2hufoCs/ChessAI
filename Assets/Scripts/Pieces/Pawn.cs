using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Pawn : Piece
    {
        public bool disableEnPassantNextTurn;
        public bool doubleMovedLastTurn;
        
        public MoveLogic _moveLogic;
        
        public Pawn(PieceData pieceData, GameObject go, MoveLogic moveLogic)
        {
            this.pieceData = pieceData;
            this.go = go;
            _moveLogic = moveLogic;
        }

        public override Piece DeepCopy(Piece pieceToCopy, Piece pieceToOverwrite = null)
        {
            Pawn p1 = (Pawn)pieceToCopy;

            if (pieceToOverwrite == null)
            {
                Pawn pawn = new(p1.pieceData, p1.go, p1._moveLogic)
                {
                    disableEnPassantNextTurn = p1.disableEnPassantNextTurn,
                    doubleMovedLastTurn = p1.doubleMovedLastTurn
                };
                return pawn;
            }
            
            Pawn p2 = (Pawn)pieceToCopy;
            p2.pieceData = p1.pieceData;
            p2.go = p1.go;
            p2._moveLogic = p1._moveLogic;
            p2.disableEnPassantNextTurn = p1.disableEnPassantNextTurn;
            p2.doubleMovedLastTurn = p1.doubleMovedLastTurn;
            return p2;
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
            if (snappedPos.y == startingRank && moves.Count > 0)
            {
                Vector2Int doubleForwardSquarePos = Vector2Int.RoundToInt(initialPos + forwardDir * 2);
                CheckSingleMove(doubleForwardSquarePos, ref moves, initialPos, false);
                //doubleMovedLastTurn = true;
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
                    Move newMove = new Move { startSquare = snappedPos, endSquare = snappedPos + new Vector2Int(-1, Mathf.RoundToInt(forwardDir.y)), enPassantCapture = leftPawn.DeepCopy(leftPawn)};
                    newMove.preMoveEnPassantCapture = leftPawn.DeepCopy(leftPawn);
                    //Debug.Log("can make en passant, go to kill would be " + newMove.enPassantCapture);
                    moves.Add(newMove);
                }
            }
            
            // Right en passant
            if (rightPawn != null)
            {
                if (rightPawn.doubleMovedLastTurn)
                {
                    Move newMove = new Move { startSquare = snappedPos, endSquare = snappedPos + new Vector2Int(1, Mathf.RoundToInt(forwardDir.y)), enPassantCapture = rightPawn.DeepCopy(rightPawn)};
                    newMove.preMoveEnPassantCapture = rightPawn.DeepCopy(rightPawn);
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
            Move move = new Move(Vector2Int.RoundToInt(startPos), targetSquarePos, DeepCopy(this, this));
            
            Pawn prePawn = (Pawn)move.preMovePieceCopy;
            if (!haveToTake)
            {
                if (targetSquare != 0)
                    return false;
                moves.Add(move);
                return true;
            }

            PieceColor targetColor = targetSquare > 8 ? PieceColor.Black :
                targetSquare > 0 ? PieceColor.White : PieceColor.None;
            if (targetColor == PieceColor.None)
                return false;
            if (pieceData.color == targetColor)
            {
                _moveLogic._pieces[targetSquarePos].isDefended = true;
                return false;
            }

            moves.Add(move);
            return true;
        }

        Pawn GetPawn(Vector2Int pos)
        {
            if (!IsInsideBounds(pos)) return null;

            int square = _moveLogic.GetSquare(pos);
            if (square is (int)PieceType.Pawn + (int)PieceColor.White or (int)PieceType.Pawn + (int)PieceColor.Black)
                return (Pawn)_moveLogic._pieces[pos];
            return null;
        }

        public static bool IsStartingSquare(Pawn pawn, Vector2Int pos)
        {
            return (pawn.pieceData.color == PieceColor.White && BoardSettings.Instance.boardFlipped
                       ? pos.y == 6
                       : pos.y == 1) ||
                   (pawn.pieceData.color == PieceColor.Black && BoardSettings.Instance.boardFlipped
                       ? pos.y == 1
                       : pos.y == 6);
        }
        
        bool IsInsideBounds(Vector2 pos)
        {
            return pos is { x: >= 0 and < 8, y: >= 0 and < 8 };
        }
    }
}