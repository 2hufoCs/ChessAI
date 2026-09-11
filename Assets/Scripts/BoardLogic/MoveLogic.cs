using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Data;
using System.IO.MemoryMappedFiles;
using NaughtyAttributes;
using Pieces;

public enum PieceType { None, Pawn, Knight, Bishop, Rook, Queen, King }
public enum PieceColor { None = -1, White = 0, Black = 8}

public class MoveLogic : MonoBehaviour
{
    private readonly int[,] _board = new int[8, 8];
    private readonly Dictionary<Vector2, Piece> _pieces = new();
    [SerializeField] private PieceColor _colorToPlay = PieceColor.White;
    
    [Header("Main parameters")]
    [SerializeField] private string _basePositionFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    
    private Pawn _heldPawn; // Used to calculate en-passant 

    [Header("References")]
    [SerializeField] private GameObject _piecePrefab;
    [SerializeField] private List<PieceData> _piecesData = new ();
    [SerializeField] private LayerMask _pieceLayer;
    private LegalMoveLogic _legalLogic;

    [Header("Debug")] 
    [SerializeField] private bool _debugLegalMoves;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _legalLogic = LegalMoveLogic.Instance;
        
        LoadPositionFromFen(_basePositionFen);
        PrecomputedMoveData precomputedMoves = new(this);
        ComputeTargetedSquares();
    }

    void OnEnable()
    {
        ActionsBus.OnPlayerMoved += ComputeTargetedSquares;
        ActionsBus.OnPawnPromoted += PromotePawn;
    }

    void OnDisable()
    {
        ActionsBus.OnPlayerMoved -= ComputeTargetedSquares;
        ActionsBus.OnPawnPromoted -= PromotePawn;
    }

    void Update()
    {
        if (_legalLogic.heldPiece != null)
            _legalLogic.heldPiece.go.transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        StateMachine(context);
    }

    void StateMachine(InputAction.CallbackContext context)
    {
        // State 1 - None
        if (!context.performed && !context.canceled) return;
        
        Vector3 mouseScreenPos =  Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Collider2D hit = Physics2D.OverlapCircle(mouseWorldPos, .01f);
        Vector2 snappedPos = WorldToBoard(mouseWorldPos);
        Vector2Int snappedWholePos = Vector2Int.RoundToInt(snappedPos);
        
        // State 2 - dragging piece
        if (context.performed) 
        {
            Piece pieceToDrag = TryDragPiece(hit, mouseWorldPos, snappedWholePos);
            if (pieceToDrag != null)
            {
                if (pieceToDrag.pieceData.color != _colorToPlay) return;
                InitializeDraggedPiece(pieceToDrag, snappedWholePos);
                if (_debugLegalMoves) LegalMoveLogic.Instance.ShowLegalMoves();
            }
        }
        // State 3 - dropping piece
        else if (context.canceled) 
        {
            if (_legalLogic.heldPiece == null) return;

            Move moveToPlay = _legalLogic.FindPseudoLegalMove(snappedWholePos);
            if (moveToPlay.endSquare != -Vector2Int.one)
                MakeMove(_legalLogic.heldPiece, moveToPlay);
            else _legalLogic.heldPiece.go.transform.position = BoardToWorld(_legalLogic.heldPieceStartSquare);
            
            if (_debugLegalMoves) _legalLogic.HideLegalMoves();
            _legalLogic.heldPiece.go.GetComponent<SpriteRenderer>().sortingLayerName = "Pieces";
            _legalLogic.heldPiece = null;
        }
    }

    void MakeMove(Piece piece, Move move)
    {
        PieceData data = GetPieceData(piece.go.GetComponent<SpriteRenderer>().sprite);
        int value = (int)data.type + (int)data.color;
        
        _board[move.startSquare.y, move.startSquare.x] = 0;
        _board[move.endSquare.y, move.endSquare.x] = value;

        _pieces.Remove(move.startSquare);
        // If target pos is taken by other piece, kill it
        if (_pieces.TryGetValue(move.endSquare, out Piece pieceToDestroy) == piece.go)
        {
            if (pieceToDestroy != piece)
            {
                Destroy(_pieces[move.endSquare].go);
                _pieces.Remove(move.endSquare);
            }
        }
        
        SpecialPieceMoves(piece, move);
            
        _pieces.Add(move.endSquare, piece);
        
        piece.go.transform.position = BoardToWorld(move.endSquare);
        if (move.rookToCastle == null)
        {
            ActionsBus.OnPlayerMoved();
            _colorToPlay = _colorToPlay ==  PieceColor.White ? PieceColor.Black : PieceColor.White;
        }
    }

    void ComputeTargetedSquares()
    {
        // Reinitialize lists
        _legalLogic.whiteTargetedSquares.Clear();
        _legalLogic.blackTargetedSquares.Clear();
        _legalLogic.whitePseudolegalMoves.Clear();
        _legalLogic.blackPseudolegalMoves.Clear();
        
        foreach (KeyValuePair<Vector2, Piece> piece in _pieces)
        {
            // Get legal moves of piece, track targeted squares
            List<Move> moves = piece.Value.GetPseudolegalMoves(piece.Key);
            List<Vector2Int> targetPositions = new();
            foreach (Move move in moves)
                targetPositions.Add(move.endSquare);
            
            if (piece.Value.pieceData.color == PieceColor.White)
            {
                _legalLogic.whiteTargetedSquares.Add(piece.Value, targetPositions);
                _legalLogic.whitePseudolegalMoves.Add(piece.Value, moves);
            }
            else
            {
                _legalLogic.blackTargetedSquares.Add(piece.Value, targetPositions);
                _legalLogic.blackPseudolegalMoves.Add(piece.Value, moves);
            }
        }
        
        Debug.Log("finished computing targeted squares and pseudo legal moves");
    }

    void SpecialPieceMoves(Piece piece, Move move)
    {
        // Also move rook for castling
        if (move.rookToCastle != null)
        {
            Vector2Int startPos = Vector2Int.FloorToInt(WorldToBoard(move.rookToCastle.go.transform.position));
            Move rookMove = new Move { startSquare = startPos, endSquare = move.rookEndSquare };
            MakeMove(move.rookToCastle, rookMove);
        }
        
        // Prevent castling if either king or rook moved
        if (piece.pieceData.type == PieceType.Rook)
        {
            Rook rook = (Rook)piece;
            rook.hasMoved = true;
        }

        if (piece.pieceData.type == PieceType.King)
        {
            King king = (King)piece;
            king.hasMoved = true;
        }
        
        // Destroy piece when en-passant
        GameObject pawnToDestroy = move.enPassantCapture;
        if (pawnToDestroy != null)
        {
            _pieces.Remove(WorldToBoard(pawnToDestroy.transform.position));
            int pawnOffset = piece.pieceData.color == PieceColor.White ? -1 : 1;
            _board[move.endSquare.y + pawnOffset, move.endSquare.x] = 0;
            Destroy(move.enPassantCapture);
        }
        
        // Remove option to en-passant pawns
        if (piece.GetType() == typeof(Pawn))
        {
            _heldPawn = (Pawn)_legalLogic.heldPiece;
            if (_heldPawn.disableEnPassantNextTurn)
                _heldPawn.doubleMovedLastTurn = false;
            else _heldPawn.disableEnPassantNextTurn = true;
        }
        
        // Pawn promotion: make player replay, have to promote to a more important piece
    }
    
    Piece TryDragPiece(Collider2D hit, Vector2 mouseWorldPos, Vector2 snappedWholePos)
    {
        if (!hit)
            return null;

        if (!IsInsideBounds(mouseWorldPos)) return null;
        
        if (_pieces.TryGetValue(snappedWholePos, out Piece pieceToDrag))
            return pieceToDrag;
        return null;
    }

    void InitializeDraggedPiece(Piece pieceToDrag, Vector2Int snappedWholePos)
    {
        _legalLogic.heldPiece = _legalLogic.goToPieces[pieceToDrag.go];
        _legalLogic.heldPiece.go.GetComponent<SpriteRenderer>().sortingLayerName = "Overlays";
        _legalLogic.heldPieceStartSquare = snappedWholePos;
                
        
        _legalLogic.heldPieceLegalMoves = _colorToPlay == PieceColor.White ?  _legalLogic.whitePseudolegalMoves[_legalLogic.heldPiece] : 
            _legalLogic.blackPseudolegalMoves[_legalLogic.heldPiece];
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

    void SpawnPiece(PieceData piece,  Vector2Int pos)
    {
        Vector2 worldPos = BoardToWorld(pos);
        GameObject newGo = Instantiate(_piecePrefab, worldPos, Quaternion.identity, transform);
        newGo.name = piece.type.ToString();

        PieceData pieceData = GetPieceData(piece);
        newGo.GetComponent<SpriteRenderer>().sprite = pieceData.sprite;

        if (_pieces.ContainsKey(pos))
        {
            Debug.Log("destroying piece as it's spawned");
            Destroy(_pieces[pos].go);
            _pieces.Remove(pos);
        }

        Piece newPiece = GetPieceFromGameObject(newGo);
        _pieces.Add(pos, newPiece);
        
        if (newPiece.pieceData.color == PieceColor.White)
            _legalLogic.whiteTargetedSquares.Add(newPiece, new List<Vector2Int>());
        else _legalLogic.blackTargetedSquares.Add(newPiece, new List<Vector2Int>());
        _legalLogic.goToPieces.Add(newGo, newPiece);
    }
    
    Piece GetPieceFromGameObject(GameObject pieceObject)
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
                newPiece = new Pawn(data, pieceObject, this, _pieces);
                break;
            case PieceType.Knight:
                newPiece = new Knight(data, pieceObject, this);
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
                newPiece = new King(data, pieceObject, this, GetRooks(data.color));
                break;
        }
                
        return newPiece;
    }

    Dictionary<Vector2, Rook> GetRooks(PieceColor color)
    {
        Dictionary<Vector2, Rook> result = new Dictionary<Vector2, Rook>();
        foreach (KeyValuePair<Vector2, Piece> piece in _pieces)
        {
            if (piece.Value.pieceData.type == PieceType.Rook && piece.Value.pieceData.color == color)
                result.Add(piece.Key, (Rook)piece.Value);
        }
        return result;
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

    void PromotePawn(PieceType newType)
    {
        Pawn pawn = (Pawn)_legalLogic.heldPiece;
        Vector2Int pos = Vector2Int.RoundToInt(WorldToBoard(_legalLogic.heldPiece.go.transform.position));

        // Pawn is trans now
        pawn.pieceData.type = newType;
        _board[pos.y, pos.x] = (int)pawn.pieceData.type + (int)pawn.pieceData.color;
        pawn.go.GetComponent<SpriteRenderer>().sprite = pawn.pieceData.sprite;
    }

    bool IsPieceEquivalent(Piece piece1, Piece piece2)
    {
        return piece1.go == piece2.go;
    }
    
    void LoadPositionFromFen(string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
    {
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
}

public struct Move : IEquatable<Move>
{
    public Vector2Int startSquare;
    public Vector2Int endSquare;
    
    // When taking a piece using en passant
    public GameObject enPassantCapture; 
    
    // For castling
    public Rook rookToCastle;
    public Vector2Int rookEndSquare;
    

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