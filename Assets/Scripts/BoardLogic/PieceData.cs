using UnityEngine;

[CreateAssetMenu(fileName = "New Piece", menuName = "Piece")]
public class PieceData : ScriptableObject
{
    public PieceType type;
    public PieceColor color;
    public Sprite sprite;
}
