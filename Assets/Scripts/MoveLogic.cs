using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using NaughtyAttributes;

public enum PieceType { None, Pawn, Knight, Bishop, Rook, Queen, King }
public enum PieceColor { White = 0, Black = 8}

public class MoveLogic : MonoBehaviour
{
    private int[,] _board = new int[8, 8];
    private Dictionary<Vector2, GameObject> _pieces = new();

    [SerializeField] private GameObject _piecePrefab;
    [SerializeField] private List<PieceData> _piecesData = new ();

    private GameObject _heldPiece;
    private Move _heldPieceMove;
    [SerializeField] private LayerMask _pieceLayer;
    [SerializeField] private string _basePositionFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //SpawnPiece(piecesData[0], new Vector2(2, 3));
        LoadPositionFromFen(_basePositionFen);
    }

    void Update()
    {
        if (_heldPiece)
            _heldPiece.transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (!context.performed && !context.canceled) return;
        
        Vector3 mouseScreenPos =  Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, mouseWorldPos, float.PositiveInfinity, _pieceLayer);
        Vector2 snappedPos = WorldToBoard(mouseWorldPos);
        
        if (context.performed)
        {
            if (!hit.collider) return;

            if (!IsInsideBounds(mouseWorldPos)) return;
            
            if (_pieces.TryGetValue(snappedPos, out GameObject piece))
            {
                Vector2Int snappedWholePos = Vector2Int.RoundToInt(snappedPos);
                _heldPiece = piece;
                _heldPieceMove = new()
                {
                    startSquare = snappedWholePos
                };
                
                Debug.Log($"raycast just hit {hit.collider.name}, matrix square has {_board[snappedWholePos.x, snappedWholePos.y]}]");
            }
        }
        else if (context.canceled)
        {
            if (!_heldPiece) return;
            snappedPos = ClampPieceBoardPos(snappedPos);
            _heldPieceMove.endSquare = Vector2Int.RoundToInt(snappedPos);
            MakeMove(_heldPiece, _heldPieceMove);
            _heldPiece = null;
        }
    }

    void MakeMove(GameObject piece, Move move)
    {
        PieceData data = GetPieceData(piece.GetComponent<SpriteRenderer>().sprite);
        int value = (int)data.type + (int)data.color;
        
        Debug.Log("supposed to be the pawn: " + _board[move.startSquare.x, move.startSquare.y]);
        _board[move.startSquare.y, move.startSquare.x] = 0;
        _board[move.endSquare.y, move.endSquare.x] = value;
        Debug.Log($"move to {move.endSquare}, started from {move.startSquare}");
        piece.transform.position = BoardToWorld(move.endSquare);
    }

    // Vector2Int FindPiece(PieceType id, PieceColor color)
    // {
    //     for (int col = 0; col < 8; col++)
    //     {
    //         for (int row = 0; row < 8; row++)
    //         {
    //             if (_board[row, col] == (int)id + (int)color)
    //                 return new Vector2Int(row, col);
    //         }
    //     }
    //     return -Vector2Int.one;
    // }

    void SpawnPiece(PieceData piece,  Vector2 pos)
    {
        Vector2 worldPos = BoardToWorld(pos);
        GameObject newPiece = Instantiate(_piecePrefab, worldPos, Quaternion.identity, transform);
        newPiece.name = piece.type.ToString();

        PieceData pieceData = GetPieceData(piece);
        newPiece.GetComponent<SpriteRenderer>().sprite = pieceData.sprite;

        if (_pieces.ContainsKey(pos))
        {
            Debug.Log("overriding square with existing piece");
            Destroy(_pieces[pos]);
            _pieces.Remove(pos);
        }
        _pieces.Add(pos, newPiece);
    }

    PieceData GetPieceData(PieceData piece)
    {
        foreach (PieceData pieceData in _piecesData)
        {
            if (pieceData.type == piece.type && pieceData.color == piece.color) return pieceData;
        }

        return null;
    }
    
    PieceData GetPieceData(int pieceType, int color)
    {
        foreach (PieceData pieceData in _piecesData)
        {
            if ((int)pieceData.type == pieceType && (int)pieceData.color == color) return pieceData;
        }

        Debug.Log($"no piece data found for type {pieceType}, color {color}");
        return null;
    }

    PieceData GetPieceData(Sprite sprite)
    {
        foreach (PieceData pieceData in _piecesData)
        {
            if (sprite == pieceData.sprite) return pieceData;
        }

        return null;
    }

    bool IsInsideBounds(Vector2 pos)
    {
        return Mathf.Abs(pos.x - transform.position.x) < 4 && Mathf.Abs(pos.y - transform.position.y) < 4;
    }

    Vector2 ClampPieceBoardPos(Vector2 pos)
    {
        return new Vector2(Mathf.Clamp(pos.x, 0, 7), Mathf.Clamp(pos.y, 0, 7));
    }

    Vector2 BoardToWorld(Vector2 pos)
    {
        return pos + (Vector2)transform.position - Vector2.one * 3.5f;
    }

    Vector2 WorldToBoard(Vector2 pos)
    {
        Vector2 snappedPos = new Vector2(pos.x > 0 ? (int)pos.x + 1 : (int)pos.x, pos.y > 0 ? (int)pos.y + 1 : (int)pos.y);
        return snappedPos + (Vector2)transform.position + Vector2.one * 3;
    }
    
    void LoadPositionFromFen(string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
    {
        Debug.Log("loading fen position");
        var pieceTypeFromSymbol = new Dictionary<char, int>()
        {
            ['k'] = (int)PieceType.King, ['p'] = (int)PieceType.Pawn, ['n'] = (int)PieceType.Knight,
            ['b']  = (int)PieceType.Bishop, ['r'] = (int)PieceType.Rook, ['q'] =  (int)PieceType.Queen
        };

        string fenBoard = fen.Split(' ')[0];
        int file = 0, rank = 7;

        foreach (char symbol in fenBoard)
        {
            if (symbol == '/')
            {
                file = 0;
                rank--;
                continue;
            }
            if (char.IsDigit(symbol))
                file += (int) char.GetNumericValue(symbol);
            else
            {
                int pieceColor = (char.IsUpper(symbol)) ? (int)PieceColor.White : (int)PieceColor.Black;
                int pieceType = pieceTypeFromSymbol[char.ToLower(symbol)];
                
                _board[rank, file] = pieceType + pieceColor;
                SpawnPiece(GetPieceData(pieceType, pieceColor), new Vector2(file, rank));
                file++;
            }
        }
        PrintBoardMatrix();
    }

    [Button]
    void PrintBoardMatrix()
    {
        string msg = "";
        for (int i = 7; i >= 0; i--)
        {
            for (int j = 0; j < 8; j++)
            {
                msg += (PieceType)_board[i, j] + ", ";
            }

            msg += "\n";
        }
        Debug.Log(msg);
    }
}

public struct Move
{
    public Vector2Int startSquare;
    public Vector2Int endSquare;
}