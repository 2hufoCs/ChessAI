using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using Pieces;
using UnityEngine;

public class Minmax : MonoBehaviour
{
    [SerializeField] private int _maxDepth;
    [HideInInspector] public int totalPositions;
    
    [Header("References")]
    [SerializeField] private LegalMoveLogic _legalGenerator;
    [SerializeField] private MoveLogic _moveLogic;

    private DateTime legalMoveTime;
    private DateTime makeMoveTime;
    private DateTime unmakeMoveTime;

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
        var initialTime = DateTime.Now;

        Dictionary<Piece, List<Move>> moves = _legalGenerator.GetAllLegalMoves(_moveLogic._pieces, _moveLogic._colorToPlay);
        int numPositions = 0;
        
        legalMoveTime += DateTime.Now - initialTime;

        foreach (KeyValuePair<Piece, List<Move>> pieceMoves in moves)
        {
            for (int i = 0; i < pieceMoves.Value.Count; i++)
            {
                Move move =  pieceMoves.Value[i];
                
                initialTime = DateTime.Now;
                
                // 1 - Play move, and update move accordingly
                move = _moveLogic.MakeMove(pieceMoves.Key, move, false);
                moves[pieceMoves.Key][i] = move;
                
                makeMoveTime += DateTime.Now - initialTime;
                
                // 2 - Calculate move count recursively from this new position
                numPositions += GetMoveCount(depth - 1);
                
                initialTime = DateTime.Now;
                
                // 3 - Unmake move
                _moveLogic.UnmakeMove(pieceMoves.Key, move, false);
                
                unmakeMoveTime += DateTime.Now - initialTime;
            }
        }

        return numPositions;
    }
    
    [Button]
    void PerformanceTest()
    {
        Dictionary<Piece, List<Move>> moves = _legalGenerator.GetAllLegalMoves(_moveLogic._pieces, _moveLogic._colorToPlay);
        int numPositions = 0;

        foreach (KeyValuePair<Piece, List<Move>> pieceMoves in moves)
        {
            int makeMoveMilliseconds = 0;
            int unmakeMoveMilliseconds = 0;
            for (int i = 0; i < 1000000; i++)
            {
                Move move =  pieceMoves.Value[0];
                
                DateTime initialTime = DateTime.Now;
                
                // 1 - Play move, and update move accordingly
                move = _moveLogic.MakeMove(pieceMoves.Key, move, false);
                moves[pieceMoves.Key][0] = move;
                
                TimeSpan diff = DateTime.Now - initialTime;
                makeMoveMilliseconds += diff.Seconds * 1000 + diff.Milliseconds;  
                
                initialTime = DateTime.Now;
                
                // 3 - Unmake move
                _moveLogic.UnmakeMove(pieceMoves.Key, move, false);
                
                diff = DateTime.Now - initialTime;
                unmakeMoveMilliseconds += diff.Seconds * 1000 + diff.Milliseconds;  
            }
            Debug.Log($"time taken moving pieces: {makeMoveMilliseconds} milliseconds");
            Debug.Log($"time taken unmoving pieces: {unmakeMoveMilliseconds} milliseconds");
        }
    }

    
    public void GetPerfInfo()
    {
        Debug.Log("legal moves time: " +  legalMoveTime.Second * 1000 +  legalMoveTime.Millisecond);
        Debug.Log("make moves time: " +  makeMoveTime.Second * 1000 +  makeMoveTime.Millisecond);
        Debug.Log("unmake moves time: " +  unmakeMoveTime.Second * 1000 +  unmakeMoveTime.Millisecond);
        
        _legalGenerator.GetLegalMovePerfInfo();
    }

}
