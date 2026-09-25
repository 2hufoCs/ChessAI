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

    private Dictionary<Move, int> depth1MoveCount = new();

    private DateTime legalMoveTime;
    private DateTime makeMoveTime;
    private DateTime unmakeMoveTime;

    public IEnumerator GetOptimizedMoveCount(int depth)
    {
        depth1MoveCount.Clear();
        _moveLogic.HideVisualsBeforeComputing();
        yield return new WaitForEndOfFrame();
        
        totalPositions = GetMoveCount(depth);
        _moveLogic.ShowVisualsAfterComputing();
    }

    private int GetMoveCount(int depth)
    {
        if (depth == 0) 
            return 1;
        var initialTime = DateTime.Now;

        Dictionary<Piece, List<Move>> moves = _legalGenerator.GetAllLegalMoves(_moveLogic.Pieces, _moveLogic.ColorToPlay);
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
                int newMoveCount = GetMoveCount(depth - 1);
                numPositions += newMoveCount;
                
                initialTime = DateTime.Now;
                
                // 3 - Unmake move
                _moveLogic.UnmakeMoveState();
                
                unmakeMoveTime += DateTime.Now - initialTime;
            }
        }

        return numPositions;
    }

    
    public void GetPerfInfo()
    {
        Debug.Log("legal moves time: " +  legalMoveTime.Second * 1000 +  legalMoveTime.Millisecond);
        Debug.Log("make moves time: " +  makeMoveTime.Second * 1000 +  makeMoveTime.Millisecond);
        Debug.Log("unmake moves time: " +  unmakeMoveTime.Second * 1000 +  unmakeMoveTime.Millisecond);
        
        _legalGenerator.GetLegalMovePerfInfo();
    }

    public void DebugMoveCount()
    {
        string msg = "";
        foreach (KeyValuePair<Move, int> move in depth1MoveCount)
            msg += $"{CoordIntToString(move.Key.startSquare)}{CoordIntToString(move.Key.endSquare)}: {move.Value}\n";
        Debug.Log(msg);
    }

    string CoordIntToString(Vector2Int coord)
    {
        char col = coord.x == 0 ? 'a' : coord.x == 1 ? 'b' : 
            coord.x == 2 ? 'c' : coord.x == 3 ? 'd' : 
            coord.x == 4 ? 'e' : coord.x == 5 ? 'f' : 
            coord.x == 6 ? 'g' :  'h';
        return col + (coord.y + 1).ToString();
    }

}