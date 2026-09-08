using UnityEngine;
using System.Collections.Generic;

public enum PieceID { None, Pawn, Knight, Bishop, Rook, Queen, King }
public enum PieceColor { White = 8, Black = 16}

public class MoveLogic : MonoBehaviour
{
    private int[,] _board = new int[8, 8];

    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private List<Piece> piecesData = new ();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SpawnPiece(piecesData[0], new Vector2(2, 3));
    }

    Vector2Int FindPiece(PieceID id, PieceColor color)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (_board[j, i] == (int)id + (int)color)
                    return new Vector2Int(j, i);
            }
        }
        return -Vector2Int.one;
    }

    void SpawnPiece(Piece piece,  Vector2 pos)
    {
        pos = BoardToWorld(pos);
        GameObject newPiece = Instantiate(piecePrefab, pos, Quaternion.identity);
        newPiece.name = piece.ToString();

        Piece pieceData = GetPieceData(piece);
        newPiece.GetComponent<SpriteRenderer>().sprite = pieceData.sprite;
    }

    Piece GetPieceData(Piece piece)
    {
        foreach (Piece pieceData in piecesData)
        {
            if (pieceData.id == piece.id && pieceData.color == piece.color) return pieceData;
        }

        return null;
    }

    Vector2 BoardToWorld(Vector2 pos)
    {
        return pos + (Vector2)transform.position - Vector2.one * 3.5f;
    }
}