using System.Collections.Generic;
using UnityEngine;

public class Minmax : MonoBehaviour
{
    [SerializeField] private int _maxDepth;
    
    [Header("References")]
    [SerializeField] private LegalMoveLogic _legalGenerator;
    [SerializeField] private MoveLogic _moveLogic;

    int GetMoveCount(int depth)
    {
        if (depth == 0) 
            return 1;

        Dictionary<Piece, List<Move>> moves = _legalGenerator.GetAllLegalMoves(_moveLogic._pieces,
            _moveLogic._colorToPlay == PieceColor.White ? _moveLogic._whiteKing : _moveLogic._blackKing);
        int numPositions = 0;

        foreach (KeyValuePair<Piece, List<Move>> pieceMoves in moves)
        {
            foreach (Move move in pieceMoves.Value)
            {
                _moveLogic.MakeMove(pieceMoves.Key, move);
                numPositions += GetMoveCount(depth - 1);
                _moveLogic.UnmakeMove(pieceMoves.Key, move);
            }
        }

        return numPositions;
    }
}
