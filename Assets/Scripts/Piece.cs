using UnityEngine;

[CreateAssetMenu(fileName = "New Piece", menuName = "Piece")]
public class Piece : ScriptableObject
{
    public PieceID id;
    public PieceColor color;
    public Sprite sprite;
}
