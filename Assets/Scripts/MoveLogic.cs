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
    [SerializeField] private Transform squareHighlighterTransform;
    
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
        // RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, float.PositiveInfinity, _pieceLayer);
        Collider2D hit = Physics2D.OverlapCircle(mouseWorldPos, .01f);
        Vector2 snappedPos = WorldToBoard(mouseWorldPos);
        //
        // DrawArrow.ForDebug(mouseWorldPos, Vector2.zero, Color.blue);
        // Debug.Break();
        
        if (context.performed)
        {
            if (!hit)
            {
                Debug.Log("didn't hit anything");
                return;
            }

            if (!IsInsideBounds(mouseWorldPos)) return;
            
            Vector2Int snappedWholePos = Vector2Int.RoundToInt(snappedPos);
            squareHighlighterTransform.position = BoardToWorld(snappedPos);
            if (_pieces.TryGetValue(snappedWholePos, out GameObject piece))
            {
                _heldPiece = piece;
                _heldPiece.GetComponent<SpriteRenderer>().sortingLayerName = "Overlays";
                _heldPieceMove = new()
                {
                    startSquare = snappedWholePos
                };
            }
            else
                hit.transform.parent = null;
        }
        else if (context.canceled)
        {
            if (!_heldPiece) return;
            snappedPos = ClampPieceBoardPos(snappedPos);
            _heldPieceMove.endSquare = Vector2Int.RoundToInt(snappedPos);
            MakeMove(_heldPiece, _heldPieceMove);
            
            _heldPiece.GetComponent<SpriteRenderer>().sortingLayerName = "Pieces";
            _heldPiece = null;
        }
    }

    void MakeMove(GameObject piece, Move move)
    {
        PieceData data = GetPieceData(piece.GetComponent<SpriteRenderer>().sprite);
        int value = (int)data.type + (int)data.color;
        
        _board[move.startSquare.y, move.startSquare.x] = 0;
        _board[move.endSquare.y, move.endSquare.x] = value;

        _pieces.Remove(move.startSquare);
        // If target pos is taken by other piece, kill it
        if (_pieces.TryGetValue(move.endSquare, out GameObject objectToDestroy) == piece)
        {
            Destroy(_pieces[move.endSquare]);
            _pieces.Remove(move.endSquare);
        }
            
        _pieces.Add(move.endSquare, piece);
        
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

    void SpawnPiece(PieceData piece,  Vector2Int pos)
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
                SpawnPiece(GetPieceData(pieceType, pieceColor), new Vector2Int(file, rank));
                file++;
            }
        }
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

    [Button]
    void PrintPieces()
    {
        // string msg = "";
        // Debug.Log($"there are {_pieces.Count} pieces");
        // foreach (Vector2 pos in _pieces.Keys)
        // {
        //     msg += 
        // }
    }
}

public static class PrecomputedMoveData
{
    public static readonly Vector2[] directionOffsets = 
    {
        Vector2.right, Vector2.up, Vector2.down, Vector2.left, 
        new Vector2(1, 1), new Vector2(-1, 1), new Vector2(-1, -1), new Vector2(1, -1) 
    };
    public static readonly int[][] numSquaresToEdge;

    static PrecomputedMoveData()
    {
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                int numNorth = 7 - rank;
                int numSouth = rank;
                int numWest = file;
                int numEast = 7 - file;

                int squareIndex = rank * 8 + file;

                int[] squaresInfo = 
                { 
                    numNorth, numSouth, numWest, numEast, 
                    Mathf.Min(numNorth, numWest), Mathf.Min(numSouth, numEast), Mathf.Min(numNorth, numEast), Mathf.Min(numSouth, numWest) 
                };
                numSquaresToEdge[squareIndex] = squaresInfo;
            }
        }    
    }
}

public struct Move
{
    public Vector2Int startSquare;
    public Vector2Int endSquare;
}