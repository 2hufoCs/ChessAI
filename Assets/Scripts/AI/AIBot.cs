using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using Pieces;
using UnityEngine;

public class AIBot : MonoBehaviour
{
    [SerializeField] private int _maxDepth;
    [HideInInspector] public int totalPositions;
    
    [Header("References")]
    [SerializeField] private LegalMoveLogic _legalGenerator;
    [SerializeField] private MoveLogic _moveLogic;

    private List<DebugMove> _debugMoves = new();
    private DebugMove _storedDict;
    //private List<List<int>> depthMoveCount = new();

    private DateTime legalMoveTime;
    private DateTime makeMoveTime;
    private DateTime unmakeMoveTime;

    private bool _debug = false;
    private bool _debugThisBranch = false;

    public IEnumerator GetOptimizedMoveCount(int depth)
    {
        _debug = depth == _maxDepth;
        _debugThisBranch = false;
        
        _debugMoves.Clear();
        _storedDict = null;
        for (int i = 0; i < depth; i++)
        {
            // _debugMoves.Add(new DebugMove(new Move(-Vector2Int.one, -Vector2Int.one, null), 0, null));
            //depthMoveCount.Add(new List<int>());
        }
        
        _moveLogic.HideVisualsBeforeComputing();
        yield return new WaitForEndOfFrame();
        
        totalPositions = GetMoveCount(depth);
        _moveLogic.ShowVisualsAfterComputing();
    }

    private int GetMoveCount(int depth, DebugMove parentDebugMove = null)
    {
        if (depth == 0) return 1;
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
                
                //Debug.Log($"depth {depth}, before making move: {_moveLogic.ColorToPlay}");
                move = _moveLogic.MakeMove(pieceMoves.Key, move, false, depth != 1);
                moves[pieceMoves.Key][i] = move;
                
                makeMoveTime += DateTime.Now - initialTime;
                
                // Get new debug move
                DebugMove newMove = null;
                //Debug.Log($"stats; _debug = {_debug}, depth = {depth != _maxDepth}, move end square = {move.endSquare == new Vector2Int(3, 4)}");
                if (depth == _maxDepth && move.endSquare == new Vector2Int(3, 4)) _debugThisBranch = true;
                if (_debugThisBranch && depth != _maxDepth)
                {
                    newMove = new(move, depth);
                    _debugMoves.Add(newMove);
                    if (parentDebugMove != null) parentDebugMove.nestedDebugMove = newMove;
                }
                
                // 2 - Calculate move count recursively from this new position
                int newMoveCount = GetMoveCount(depth - 1, newMove);
                numPositions += newMoveCount;
                if (newMove != null && _debug) newMove.moveCount += newMoveCount;

                if (depth != 1)
                {
                    // if (depth == 4)
                    // {
                    //     _debugMoves.Add();
                    // }
                    //depthMoves[depth - 1].Add(move); // Add move
                }
                
                initialTime = DateTime.Now;
                
                // 3 - Unmake move
                if (depth != 1) _moveLogic.UnmakeMoveState();
                else BoardState.boardStates.Peek().LoadBoardState();
                
                unmakeMoveTime += DateTime.Now - initialTime;
            }
            //_moveLogic.ColorToPlay = _moveLogic.ColorToPlay == PieceColor.White ? PieceColor.Black : PieceColor.White;
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
        Debug.Log("debug count: " + _debugMoves.Count);
        for (int i = 0; i < _debugMoves.Count; i++)
        {
            // Make tabs depending on depth
            for (int j = _debugMoves[i].depth; j < _maxDepth; j++)
                msg += "\t";
            msg += $"{CoordIntToString(_debugMoves[i].playedMove.startSquare)}{CoordIntToString(_debugMoves[i].playedMove.endSquare)}: {_debugMoves[i].moveCount}\n";
        }
            
        System.IO.File.WriteAllText("C:\\Users\\c.clement\\Desktop\\DebugLog.txt", msg);
        Debug.Log("finished writing content to text file");
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

public class DebugMove
{
    public Move playedMove;
    public int moveCount;
    public DebugMove nestedDebugMove;

    public int depth;

    // public DebugMove(Move playedMove, int moveCount, DebugMove nestedDebugMove)
    // {
    //     this.playedMove = playedMove;
    //     this.moveCount = moveCount;
    //     this.nestedDebugMove = nestedDebugMove;
    // }

    public DebugMove(Move playedMove, int depth)
    {
        this.playedMove = playedMove;
        this.depth = depth;
    }
}