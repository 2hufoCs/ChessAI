using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Minmax : MonoBehaviour
{
    [SerializeField] private int _maxDepth;
    [HideInInspector] public int totalPositions;
    
    [Header("References")]
    [SerializeField] private LegalMoveLogic _legalGenerator;
    [SerializeField] private MoveLogic _moveLogic;

    public IEnumerator GetOptimizedMoveCount(int depth)
    {
        _moveLogic.HideVisualsBeforeMinmax();
        yield return new WaitForEndOfFrame();
        
        totalPositions = GetMoveCount(depth);
        _moveLogic.ShowVisualsAfterMinmax();
    }

    private int GetMoveCount(int depth)
    {
        if (depth == 0) 
            return 1;

        Dictionary<Piece, List<Move>> moves = _legalGenerator.GetAllLegalMoves(_moveLogic._pieces,
            _moveLogic._colorToPlay == PieceColor.White ? _moveLogic._whiteKing : _moveLogic._blackKing);
        int numPositions = 0;

        foreach (KeyValuePair<Piece, List<Move>> pieceMoves in moves)
        {
            for (int i = 0; i < pieceMoves.Value.Count; i++)
            {
                Move move =  pieceMoves.Value[i];
                
                // DateTime initialTime = DateTime.Now;
                
                // 1 - Play move, and update move accordingly
                move = _moveLogic.MakeMove(pieceMoves.Key, move, false);
                moves[pieceMoves.Key][i] = move;

                // if (depth == 1)
                // {
                //     TimeSpan diff = DateTime.Now - initialTime;
                //     float timeDiff = diff.Seconds * 1000 + diff.Milliseconds;  
                //     Debug.Log("time taken making move: " + timeDiff);
                // }
                
                // 2 - Calculate move count recursively from this new position
                numPositions += GetMoveCount(depth - 1);
                
                // initialTime = DateTime.Now;
                
                // 3 - Unmake move
                _moveLogic.UnmakeMove(pieceMoves.Key, move, false);
                
                // if (depth == 1)
                // {
                //     TimeSpan diff = DateTime.Now - initialTime;
                //     float timeDiff = diff.Seconds * 1000 + diff.Milliseconds;  
                //     Debug.Log("time taken unmaking move: " + timeDiff);
                // }
            }
        }

        return numPositions;
    }
}
