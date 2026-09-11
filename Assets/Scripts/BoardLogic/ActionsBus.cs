using System;
using UnityEngine;

public class ActionsBus : MonoBehaviour
{
    public static Action OnPlayerMoved;
    
    public static Action<PieceType> OnPawnPromoted;
}
