using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using NaughtyAttributes;
using Pieces;

public enum PieceType { None, Pawn, Knight, Bishop, Rook, Queen, King }
public enum PieceColor { White = 0, Black = 8}

public class MoveLogic : MonoBehaviour
{
    private int[,] _board = new int[8, 8];
    private Dictionary<Vector2, GameObject> _pieces = new();
    
    [Header("Main parameters")]
    [SerializeField] private string _basePositionFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    
    private Piece _heldPiece;
    private Move _heldPieceMove;

    [Header("References")]
    [SerializeField] private GameObject _piecePrefab;
    [SerializeField] private List<PieceData> _piecesData = new ();
    [SerializeField] private LayerMask _pieceLayer;

    [Header("Debug")] 
    [SerializeField] private bool _debugLegalMoves;
    [SerializeField] private GameObject _squareHighlighterPrefab;
    private List<GameObject> _squareHighlighters = new();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadPositionFromFen(_basePositionFen);
        PrecomputedMoveData precomputedMoves = new(this);
    }

    void Update()
    {
        if (_heldPiece != null)
            _heldPiece.go.transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (!context.performed && !context.canceled) return;
        
        Vector3 mouseScreenPos =  Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Collider2D hit = Physics2D.OverlapCircle(mouseWorldPos, .01f);
        Vector2 snappedPos = WorldToBoard(mouseWorldPos);
        
        if (context.performed) // Start piece drag
        {
            if (!hit)
                return;

            if (!IsInsideBounds(mouseWorldPos)) return;
            
            Vector2Int snappedWholePos = Vector2Int.RoundToInt(snappedPos);
            if (_pieces.TryGetValue(snappedWholePos, out GameObject pieceObject))
            {
                _heldPiece = GetPieceFromRaycast(pieceObject);
                _heldPiece.go.GetComponent<SpriteRenderer>().sortingLayerName = "Overlays";
                _heldPieceMove = new()
                {
                    startSquare = snappedWholePos
                };
                
                // Show legal moves in debug
                if (_debugLegalMoves)
                    ShowLegalMoves(_heldPiece);
            }
        }
        else if (context.canceled) // Stop piece drag
        {
            if (_heldPiece != null) return;
            snappedPos = ClampPieceBoardPos(snappedPos);
            _heldPieceMove.endSquare = Vector2Int.RoundToInt(snappedPos);

            if (CheckForLegalMove(_heldPiece, _heldPieceMove))
            {
                MakeMove(_heldPiece.go, _heldPieceMove);
            
                _heldPiece.go.GetComponent<SpriteRenderer>().sortingLayerName = "Pieces";
                _heldPiece = null;
            }
        }
    }

    Piece GetPieceFromRaycast(GameObject pieceObject)
    {
        // Get corresponding data of released piece
        Sprite sprite = pieceObject.GetComponent<SpriteRenderer>().sprite;
        PieceData data = _piecesData[0];
        foreach (PieceData pieceData in _piecesData)
        {
            if (pieceData.sprite == sprite)
            {
                data = pieceData;
                break;
            }
        }
                
        // Create a new piece with obtained data
        Piece newPiece = null;
        switch (data.type)
        {
            case PieceType.Pawn:
                newPiece = new Pawn(data, pieceObject);
                break;
            case PieceType.Knight:
                newPiece = new Knight(data, pieceObject);
                break;
            case PieceType.Bishop:
                newPiece = new Bishop(data, pieceObject);
                break;
            case PieceType.Rook:
                newPiece = new Rook(data, pieceObject);
                break;
            case PieceType.Queen:
                newPiece = new Queen(data, pieceObject);
                break;
            case PieceType.King:
                newPiece = new King(data, pieceObject);
                break;
        }
                
        return newPiece;
    }

    bool CheckForLegalMove(Piece piece, Move move)
    {
        return piece.GetLegalMoves().Contains(move);
    }

    void ShowLegalMoves(Piece piece)
    {
        // Destroy previous square highlighters
        foreach (GameObject go in _squareHighlighters)
            Destroy(go);
        _squareHighlighters.Clear();
        
        foreach (Move move in piece.GetLegalMoves())
        {
            GameObject go = Instantiate(_squareHighlighterPrefab, BoardToWorld(move.endSquare), Quaternion.identity, transform);
            _squareHighlighters.Add(go);
        }
    }

    /// <summary>
    /// Read from the _board array at the given square.
    /// </summary>
    /// <param name="square">The position in board space of the needed square.</param>
    /// <returns></returns>
    public int GetSquare(Vector2Int square)
    {
        if (square.x < 0 || square.y < 0 || square.x > 7 || square.y > 7) return -1;
        return _board[square.y, square.x];
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

public class PrecomputedMoveData
{
    public enum Directions { Right, Up, Left, Down, UpRight, UpLeft, DownLeft, DownRight}
    public static readonly Vector2Int[] directionOffsets = 
    {
        Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down,
        new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1) 
    };
    public static readonly int[,,] numSquaresToEdges = new int[8, 8, 8];

    private static MoveLogic _moveLogic;

    /// <summary>
    /// Precomputes, for each square, how much distance there is with the edge of the board, in all directions.
    /// Allows for incredible performance optimization for sliding pieces (bishops/rooks/queens) instead of recalculating each time.
    /// </summary>
    public PrecomputedMoveData(MoveLogic moveLogic)
    {
        _moveLogic = moveLogic;
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                int numNorth = 7 - rank;
                int numSouth = rank;
                int numWest = file;
                int numEast = 7 - file;
                
                int[] squaresInfo = 
                { 
                    numEast, numNorth, numWest, numSouth,
                    Mathf.Min(numNorth, numEast), Mathf.Min(numNorth, numWest), Mathf.Min(numSouth, numWest), Mathf.Min(numSouth, numEast)
                };
                for (int i = 0; i < 8; i++)
                {
                    numSquaresToEdges[file, rank, i] = squaresInfo[i];
                }
            }
        }    
    }

    public static List<Move> GenerateSlidingMoves(Piece piece)
    {
        List<Move> moves = new List<Move>();
        Vector2Int startPos = WorldToBoard(piece);
        
        for (int directionIndex = 0; directionIndex < 8; directionIndex++)
        {
            Debug.Log($"indices for num squares to edges: {startPos.x}, {startPos.y}, {directionIndex}");
            for (int n = 0; n < numSquaresToEdges[startPos.x, startPos.y, directionIndex]; n++)
            {
                Vector2Int targetSquare = startPos + directionOffsets[directionIndex] * (n + 1);
                int pieceOnTargetSquare = _moveLogic.GetSquare(targetSquare);

                PieceColor friendlyColor = piece.pieceData.color;
                
                // Blocked by friendly piece, can't move any further in that direction
                if ((pieceOnTargetSquare < 8 && friendlyColor == PieceColor.White) ||
                    (pieceOnTargetSquare > 8 && friendlyColor == PieceColor.Black))
                    break;
                
                Move move = new Move
                {
                    startSquare = startPos,
                    endSquare = targetSquare
                };
                moves.Add(move);

                // Can't move any further in this direction after capturing opponent's piece
                if ((pieceOnTargetSquare < 8 && friendlyColor == PieceColor.Black) ||
                    (pieceOnTargetSquare > 8 && friendlyColor == PieceColor.White))
                    break;
            }
        }

        return moves;
    }
    
    static Vector2Int WorldToBoard(Piece piece)
    {
        Vector2Int pos = Vector2Int.RoundToInt(piece.go.transform.position);
        Vector2Int snappedPos = new Vector2Int(pos.x > 0 ? pos.x + 1 : pos.x, pos.y > 0 ? pos.y + 1 : pos.y);
        return snappedPos + Vector2Int.RoundToInt(_moveLogic.transform.position) + Vector2Int.one * 3; // Offset due to pivot point being in center of the board
    }
}

public struct Move : IEquatable<Move>
{
    public Vector2Int startSquare;
    public Vector2Int endSquare;

    public bool Equals(Move other)
    {
        return startSquare.Equals(other.startSquare) && endSquare.Equals(other.endSquare);
    }

    public override bool Equals(object obj)
    {
        return obj is Move other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(startSquare, endSquare);
    }
}