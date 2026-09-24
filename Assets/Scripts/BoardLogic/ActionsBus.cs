using System;
using UnityEngine;

public class ActionsBus : MonoBehaviour
{
    public static Action OnPlayerMoved;
    public static Action OnPlayerUnmoved;
    
    public static Action<PieceType> OnPawnPromoted;
    
    // Endgame states
    public static Action<PieceColor> OnCheckmate;
    public static Action<DrawOutcomes> OnDraw;
}
