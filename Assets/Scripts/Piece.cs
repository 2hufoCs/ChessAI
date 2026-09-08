using UnityEngine;

[CreateAssetMenu(fileName = "New Piece", menuName = "Piece")]
public class PieceData : ScriptableObject
{
    public PieceType id;
    public PieceColor color;
    public Sprite sprite;
}
