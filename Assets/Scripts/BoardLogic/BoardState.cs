using UnityEngine;
using System.Collections.Generic;

public class BoardState
{
    static public int totalStatesCount = 0; 
    
    public readonly Stack<int[,]> boardStates = new();
    public readonly Stack<Dictionary<Vector2Int, Piece>> piecesStates = new();
    public readonly Stack<Dictionary<Piece, List<Move>>> legalMovesStates = new();
    public readonly Stack<Dictionary<GameObject, Piece>> goToPiecesStates = new(); // to reassign ???

    /// <summary>
    /// Create a new board state, using deep copies to prevent instances pointing to the same reference
    /// </summary>
    /// <param name="boardStates"></param>
    /// <param name="piecesStates"></param>
    /// <param name="legalMovesStates"></param>
    /// <param name="goToPiecesStates"></param>
    public BoardState(Stack<int[,]> boardStates, Stack<Dictionary<Vector2Int, Piece>> piecesStates, 
        Stack<Dictionary<Piece, List<Move>>> legalMovesStates, Stack<Dictionary<GameObject, Piece>> goToPiecesStates)
    {
        this.boardStates = new Stack<int[,]>(new Stack<int[,]>(boardStates));
        this.piecesStates = new Stack<Dictionary<Vector2Int, Piece>>(new Stack<Dictionary<Vector2Int, Piece>>(piecesStates));
        this.legalMovesStates = new Stack<Dictionary<Piece, List<Move>>>(new Stack<Dictionary<Piece, List<Move>>>(legalMovesStates));
        this.goToPiecesStates = new Stack<Dictionary<GameObject, Piece>>(new Stack<Dictionary<GameObject, Piece>>(goToPiecesStates));
        totalStatesCount++;
    }

    public void UndoBoardState()
    {
        boardStates.Pop();
        piecesStates.Pop();
        legalMovesStates.Pop();
        goToPiecesStates.Pop();
    }

    /// <summary>
    /// Loads the board state and show it to the board without having to recalculate legal moves
    /// </summary>
    /// <param name="boardStates"></param>
    /// <param name="piecesStates"></param>
    /// <param name="legalMovesStates"></param>
    /// <param name="goToPiecesStates"></param>
    public void LoadBoardState(ref Stack<int[,]> boardStates, ref Stack<Dictionary<Vector2Int, Piece>> piecesStates, 
        ref Stack<Dictionary<Piece, List<Move>>> legalMovesStates, ref Stack<Dictionary<GameObject, Piece>> goToPiecesStates)
    {
        //boardStates = this.boardStates;
    }
}
