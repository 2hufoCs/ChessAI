using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Data;
using System.IO.MemoryMappedFiles;
using System.Linq;
using DG.Tweening;
using NaughtyAttributes;
using Pieces;
using UnityEngine.SceneManagement;

public enum PieceType { None, Pawn, Knight, Bishop, Rook, Queen, King }
public enum PieceColor { None = -1, White = 0, Black = 8}

public class MoveLogic : MonoBehaviour
{
    private int[,] _board = new int[8, 8];
    public int[,] Board => _board;
    private Dictionary<Vector2Int, Piece> _pieces = new();
    public Dictionary<Vector2Int, Piece> Pieces => _pieces;
    private readonly Dictionary<Vector2, Piece> _piecesAliveAndDead = new();
    
    private readonly Stack<KeyValuePair<Move, Piece>> _movesPlayed = new();
    private readonly Stack<KeyValuePair<Move, Piece>> _undoMoves = new();
    
    [Header("Main parameters")] 
    [SerializeField] private PieceColor _colorToPlay = PieceColor.White;
    public PieceColor ColorToPlay =>  _colorToPlay;
    [SerializeField] private string _basePositionFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public bool IsGamePaused { get => _isGamePaused; set => Pause(value); }
    private bool _isGamePaused;
    
    // Keep track of both kings
    public King _whiteKing;
    public King _blackKing;
    private Pawn _heldPawn; // Used to calculate en-passant 

    private Vector2Int[] _whiteRooksPos;
    private Vector2Int[] _blackRooksPos;

    [Header("References")]
    [SerializeField] private GameObject _piecePrefab;
    [SerializeField] private List<PieceData> _piecesData = new ();
    [SerializeField] private LayerMask _pieceLayer;

    [SerializeField] private SpriteRenderer _pauseDarkOverlay;
    [SerializeField] private GameObject _whitePromotionChoicePrefab;
    [SerializeField] private GameObject _blackPromotionChoicePrefab;
    private GameObject _spawnedPromotionChoice;
    
    private LegalMoveLogic _legalGenerator;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _legalGenerator = LegalMoveLogic.Instance;
        BoardState.boardStates = new();
        
        InitializeBaseCastlingData();
        LoadPositionFromFen(_basePositionFen);
        _legalGenerator.whiteKing = _whiteKing;
        _legalGenerator.blackKing = _blackKing;
        
        // Precomputed stuff
        InitializeKingCastling();
        PrecomputedMoveData precomputedMoves = new(this);
        RecomputeLegalMoves();
    }

    void OnEnable()
    {
        ActionsBus.OnPlayerMoved += RecomputeLegalMoves;
        ActionsBus.OnPawnPromoted += PromotePawn;
    }

    void OnDisable()
    {
        ActionsBus.OnPlayerMoved -= RecomputeLegalMoves;
        ActionsBus.OnPawnPromoted -= PromotePawn;
    }

    void RecomputeLegalMoves()
    {
        _legalGenerator.GetAllLegalMoves(_pieces, _colorToPlay);
        BoardState newState = new(this, _legalGenerator);

        Debug.Log("new turn, " + _legalGenerator.LegalMoves.Count);
        if (_legalGenerator.LegalMoves.Count == 0)
        {
            if (_colorToPlay == PieceColor.Black ? _blackKing.isInCheck : _whiteKing.isInCheck)
                ActionsBus.OnCheckmate(_colorToPlay);
            else ActionsBus.OnDraw(DrawOutcomes.Stalemate);
        }
    }

    void InitializeBaseCastlingData()
    {
        int flipped = BoardSettings.Instance.boardFlipped ? 1 : 0;
        _whiteRooksPos = new[] { new Vector2Int(0, flipped * 7), new Vector2Int(7, flipped * 7) };
        _blackRooksPos = new[] { new Vector2Int(0, (1 - flipped) * 7), new Vector2Int(7, (1 - flipped) * 7) };
    }

    void Update()
    {
        if (_legalGenerator.heldPiece != null)
            _legalGenerator.heldPiece.go.transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        StateMachine(context);
    }

    void StateMachine(InputAction.CallbackContext context)
    {
        // State 1 - None
        //if (context.performed) Debug.Log($"just clicked in state machine, is game paused:  {_isGamePaused}, is held piece null: {_legalGenerator.heldPiece != null}");
        if (!context.performed && !context.canceled || IsGamePaused) return;
        
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
                if (BoardSettings.Instance.debugLegalMoves) LegalMoveLogic.Instance.ShowLegalMoves();
            }
        }
        // State 3 - dropping piece
        else if (context.canceled) 
        {
            if (_legalGenerator.heldPiece == null) return;

            Move moveToPlay = _legalGenerator.FindPseudoLegalMove(snappedWholePos);
            if (moveToPlay.endSquare != -Vector2Int.one)
            {
                MakeMove(_legalGenerator.heldPiece, moveToPlay);
            }
            else _legalGenerator.heldPiece.go.transform.position = BoardToWorld(_legalGenerator.heldPieceStartSquare);

            ReleasePiece();
        }
    }

    void ReleasePiece()
    {
        if (BoardSettings.Instance.debugLegalMoves) _legalGenerator.HideLegalMoves();
        _legalGenerator.heldPiece.go.GetComponent<SpriteRenderer>().sortingLayerName = "Pieces";
        _legalGenerator.heldPiece = null;
    }

    public Move MakeMove(Piece piece, Move move, bool trackMove = true)
    {
        // Before playing move, promotion check
        if (PromotionCheck(piece, move)) return move;
        
        PieceData data = GetPieceData(piece.go.GetComponent<SpriteRenderer>().sprite);
        int value = (int)data.type + (int)data.color;
        bool isPlayerHuman = data.color == PieceColor.White && BoardSettings.Instance.isWhiteHuman || data.color == PieceColor.Black && BoardSettings.Instance.isBlackHuman;
        
        _board[move.startSquare.y, move.startSquare.x] = 0;
        _board[move.endSquare.y, move.endSquare.x] = value;
        if (isPlayerHuman) piece.go.transform.position = BoardToWorld(move.endSquare);

        _pieces.Remove(move.startSquare);
        // If target pos is taken by other piece, kill it
        if (_pieces.TryGetValue(move.endSquare, out Piece pieceToDestroy) == piece.go)
        {
            if (pieceToDestroy != piece)
            {
                move.pieceOnTargetSquare = pieceToDestroy;
                if (isPlayerHuman) _pieces[move.endSquare].go.SetActive(false);
                _pieces.Remove(move.endSquare);
            }
        }
        
        SpecialPieceMoves(piece, move);
            
        _pieces.TryAdd(move.endSquare, piece);
        if (trackMove)
            TrackPlayedMove(piece, move);
        
        // If game has ended
        if (GameEndChecks(move)) return move; 
        
        if (move.rookToCastle == null)
        {
            _colorToPlay = _colorToPlay ==  PieceColor.White ? PieceColor.Black : PieceColor.White;
            if (trackMove) ActionsBus.OnPlayerMoved();
        }
        else
        {
            Vector2Int startPos = Vector2Int.FloorToInt(WorldToBoard(move.rookToCastle.go.transform.position));
            Move rookMove = new Move(startPos, move.rookEndSquare, move.rookToCastle.DeepCopy(move.rookToCastle));
            MakeMove(move.rookToCastle, rookMove);
        }
        return move;
    }

    bool GameEndChecks(Move move)
    {
        if (move.drawOutcome != DrawOutcomes.None)
        {
            ActionsBus.OnDraw(move.drawOutcome);
            return true;
        }
        
        return false;
    }

    void TrackPlayedMove(Piece piece, Move move)
    {
        Piece target = move.pieceOnTargetSquare;
        if (target != null)
            move.pieceOnTargetSquare = target.DeepCopy(target);
        Piece otherTarget = move.enPassantCapture;
        if (otherTarget != null)
        {
            move.enPassantCapture = otherTarget.DeepCopy(otherTarget);
            Pawn pawn = (Pawn)move.enPassantCapture;
            Debug.Log($"playing en passant, capture stats: {pawn.disableEnPassantNextTurn}, {pawn.doubleMovedLastTurn}");
        }
            
        _movesPlayed.Push(new KeyValuePair<Move, Piece>(move, piece));
        _undoMoves.Clear();
    }

    public void HideVisualsBeforeComputing()
    {
        foreach (KeyValuePair<Vector2Int, Piece> piece in _pieces)
        {
            piece.Value.go.SetActive(false);
        }
    }

    /// <summary>
    /// Actually computes visuals once all abstract calculations have been made, such as: enabled/disabled pieces
    /// </summary>
    public void ShowVisualsAfterComputing()
    {
        foreach (KeyValuePair<Vector2Int, Piece> piece in _pieces)
        {
            piece.Value.go.transform.position =  BoardToWorld(piece.Key);
            piece.Value.go.SetActive(true);
        }
    }

    public void UnmakeMoveState()
    {
        if (BoardState.boardStates.Count == 0)
        {
            Debug.LogError("trying to unmake move, but stack is empty");
            return;
        }

        // Remove last stored state
        if (BoardState.boardStates.Count > 1)
        {
            BoardState.UndoBoardState();
            Debug.Log("after undoing board state, e5 square is " + BoardState.boardStates.Peek().BoardValueStates[3, 4]);
        }
        //if (changeTurn) _colorToPlay =  _colorToPlay ==  PieceColor.White ? PieceColor.Black : PieceColor.White; // Don't change turn when base pos
        
        // Load previously stored board state
        BoardState.boardStates.Peek().LoadBoardState();
    }

    public void LoadBoardState(BoardState state)
    {
        _board = state.BoardValueStates.Clone() as int[,];
        
        Dictionary<Vector2Int, Piece> piecesCopy = new();
        foreach (KeyValuePair<Vector2Int, Piece> piece in state.PiecesStates)
        {
            Piece copy = piece.Value.DeepCopy(piece.Value);
            piecesCopy[piece.Key] = copy;
        }
        _pieces = piecesCopy;
        
        _colorToPlay = state.PlayerColor;
    }

    public void UnmakeLastMoveState()
    {
        UnmakeMoveState();
        ShowVisualsAfterComputing();
    }

    
    public void RedoLastMove()
    {
        if (_undoMoves.Count == 0) return;
        
        KeyValuePair<Move, Piece> move = _undoMoves.Pop();

        _movesPlayed.Push(move);
        
        MakeMove(move.Value, move.Key, false);
    }

    void SpecialPieceMoves(Piece piece, Move move)
    {
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
        if (move.enPassantCapture != null)
        {
            Pawn pawnToDestroy = (Pawn)move.enPassantCapture; // Removed deep copy
            Debug.Log("doing en passant");
            _pieces.Remove(WorldToBoard(pawnToDestroy.go.transform.position));
            int pawnOffset = piece.pieceData.color == PieceColor.White ? -1 : 1;
            _board[move.endSquare.y + pawnOffset, move.endSquare.x] = 0;
            move.enPassantCapture.go.SetActive(false);
        }
        
        if (_heldPawn != null)
        {
            //Debug.Log(piece.pieceData.type);
            // Remove option to en-passant pawns
            _heldPawn = (Pawn)piece;
            if (Mathf.Abs(move.startSquare.y - move.endSquare.y) == 2)
                _heldPawn.doubleMovedLastTurn = true;
            else if (_heldPawn.disableEnPassantNextTurn)
                _heldPawn.doubleMovedLastTurn = false;
            else _heldPawn.disableEnPassantNextTurn = true;
        }
    }

    bool PromotionCheck(Piece piece,  Move move)
    {
        if (piece.GetType() == typeof(Pawn))
        {
            // Pawn promotion: make player replay, have to promote to a more important piece
            int endRank = piece.pieceData.color == PieceColor.White ? 7 : 0;
            if (move.endSquare.y == endRank)
            {
                ShowPromotionChoice(_heldPawn, move);
                return true;
            }
        }

        return false;
    }
    
    public void RestartGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    
    Piece TryDragPiece(Collider2D hit, Vector2 mouseWorldPos, Vector2Int snappedWholePos)
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
        _legalGenerator.heldPiece = _legalGenerator.GetPieceFromGO(pieceToDrag.go);
        if (_legalGenerator.heldPiece == null) return;
        
        _legalGenerator.heldPiece.go.GetComponent<SpriteRenderer>().sortingLayerName = "Overlays";
        _legalGenerator.heldPieceStartSquare = snappedWholePos;
        
        // For promotion and en-passant
        _heldPawn = _legalGenerator.heldPiece.GetType() == typeof(Pawn) ? (Pawn)_legalGenerator.heldPiece : null;
        
        if (_legalGenerator.LegalMoves.TryGetValue(_legalGenerator.heldPiece, out List<Move> moves))
        {
            _legalGenerator.heldPieceLegalMoves = moves;
            //Debug.Log("assigning held piece legal moves at least, color: " + _legalGenerator.heldPiece.pieceData.color);
        }
        else // for debug, not supposed to happen
        {
            foreach (KeyValuePair<Piece, List<Move>> kvp in _legalGenerator.LegalMoves)
            {
                foreach (Move move in kvp.Value)
                {
                    Debug.Log($"{kvp.Key.pieceData.color} {kvp.Key.pieceData.type} from {move.startSquare} to {move.endSquare}");
                }
            }
            Debug.LogError("held piece isn't in legal moves");
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
            _pieces[pos].go.SetActive(false);
            _pieces.Remove(pos);
        }

        Piece newPiece = GetPieceFromGameObject(newGo);
        _pieces.Add(pos, newPiece);
        _piecesAliveAndDead.Add(pos, newPiece);
        
        // if (newPiece.pieceData.color == PieceColor.White)
        //     _legalGenerator._whiteTargetedSquares.Add(newPiece, new List<Vector2Int>());
        // else _legalGenerator._blackTargetedSquares.Add(newPiece, new List<Vector2Int>());
        _legalGenerator.GoToPieces.Add(newGo, newPiece);
        
        // Keep track of both kings
        if (newPiece.pieceData.type == PieceType.King)
        {
            King king = (King)newPiece;
            if (newPiece.pieceData.color == PieceColor.White)
                _whiteKing = king;
            else _blackKing = king;
        }
        
        // Check if rook or king has moved, prevent castling
        if (piece.type == PieceType.Rook)
        {
            if (piece.color == PieceColor.White && !_whiteRooksPos.Contains(pos) ||
                piece.color == PieceColor.Black && !_blackRooksPos.Contains(pos))
            {
                Debug.Log($"{piece.color} {piece.type} at {pos} not on starting square, preventing castling");
                Rook rook = (Rook)newPiece;
                rook.hasMoved = true;
            }
        }

        if (piece.type == PieceType.King)
        {
            Vector2Int whiteKingBasePos = !BoardSettings.Instance.boardFlipped ? new Vector2Int(4, 0) : new Vector2Int(4, 7);
            Vector2Int blackKingBasePos = !BoardSettings.Instance.boardFlipped ? new Vector2Int(4, 7) : new Vector2Int(4, 0);
            if (piece.color == PieceColor.White && whiteKingBasePos != pos ||
                piece.color == PieceColor.Black && blackKingBasePos != pos)
            {
                Debug.Log($"{piece.color} {piece.type} at {pos} not on starting square, preventing castling");
                King king = (King)newPiece;
                king.hasMoved = true;
            }
        }
    }

    void InitializeKingCastling()
    {
        if (_whiteKing == null)
            Debug.LogError("no white king on board");
        if (_blackKing == null)
            Debug.LogError("no black king on board");
        
        _whiteKing.AssignRooks(GetRooks(PieceColor.White));
        _blackKing.AssignRooks(GetRooks(PieceColor.Black));
    }
    
    public Piece GetPieceFromGameObject(GameObject pieceObject)
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
                
        return InitializePieceFromData(data, pieceObject);
    }

    Piece InitializePieceFromData(PieceData data, GameObject pieceObject)
    {
        // Create a new piece with obtained data
        Piece newPiece = null;
        switch (data.type)
        {
            case PieceType.Pawn:
                newPiece = new Pawn(data, pieceObject, this);
                break;
            case PieceType.Knight:
                newPiece = new Knight(data, pieceObject, this);
                break;
            case PieceType.Bishop:
                newPiece = new Bishop(data, pieceObject, this);
                break;
            case PieceType.Rook:
                newPiece = new Rook(data, pieceObject, this);
                break;
            case PieceType.Queen:
                newPiece = new Queen(data, pieceObject, this);
                break;
            case PieceType.King:
                newPiece = new King(data, pieceObject, this);
                break;
        }

        return newPiece;
    }

    Dictionary<Vector2, Rook> GetRooks(PieceColor color)
    {
        Dictionary<Vector2, Rook> result = new Dictionary<Vector2, Rook>();
        foreach (KeyValuePair<Vector2Int, Piece> piece in _pieces)
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

    Vector2Int WorldToBoard(Vector2 pos)
    {
        Vector2 snappedPos = new Vector2(pos.x > 0 ? (int)pos.x + 1 : (int)pos.x, pos.y > 0 ? (int)pos.y + 1 : (int)pos.y);
        return Vector2Int.RoundToInt(snappedPos + (Vector2)transform.position + Vector2.one * 3);
    }

    void Pause(bool pause)
    {
        _pauseDarkOverlay.enabled = pause;
        _isGamePaused = pause;
    }

    void ShowPromotionChoice(Pawn pawnToPromote, Move move)
    {
        IsGamePaused = true;
        Vector2Int endPos = move.endSquare;
        pawnToPromote.go.transform.position = BoardToWorld(endPos);

        // Remove pawn from board matrix
        Vector2Int startPos = move.startSquare;
        _board[startPos.y, startPos.x] = 0;
        _pieces.Remove(move.startSquare);
        
        if (_colorToPlay == PieceColor.White && BoardSettings.Instance.isWhiteHuman)
            _spawnedPromotionChoice = Instantiate(_whitePromotionChoicePrefab, BoardToWorld(endPos),  Quaternion.identity, transform);
        else if (_colorToPlay == PieceColor.Black && BoardSettings.Instance.isBlackHuman)
            _spawnedPromotionChoice = Instantiate(_blackPromotionChoicePrefab, BoardToWorld(endPos), Quaternion.identity, transform);
    }

    void PromotePawn(PieceType newType)
    {
        Pawn oldPawn = (Pawn)_heldPawn.DeepCopy(_heldPawn);
        Vector2Int pos = Vector2Int.RoundToInt(WorldToBoard(_heldPawn.go.transform.position));
        
        // Pawn is trans now
        PieceData data = GetPieceData((int)newType, (int)_heldPawn.pieceData.color);
        Piece promotedPawn = InitializePieceFromData(data, _heldPawn.go);
        
        _board[pos.y, pos.x] = (int)promotedPawn.pieceData.type + (int)promotedPawn.pieceData.color;
        promotedPawn.go.GetComponent<SpriteRenderer>().sprite = promotedPawn.pieceData.sprite;
        
        // Delete piece selector if human playing
        if (_spawnedPromotionChoice)
            Destroy(_spawnedPromotionChoice);
        
        // If target pos is taken by other piece, kill it
        if (_pieces.TryGetValue(pos, out Piece pieceToDestroy) == _heldPawn.go)
        {
            if (pieceToDestroy != promotedPawn)
            {
                _pieces[pos].go.SetActive(false);
                _pieces.Remove(pos);
            }
        }

        _pieces[pos] =  promotedPawn;
        
        _legalGenerator.RemovePieceFromLegalMoves(_heldPawn);
        _legalGenerator.AddPieceToLegalMoves(promotedPawn);
        _legalGenerator.GoToPieces[_heldPawn.go] = promotedPawn;
        
        _colorToPlay = _colorToPlay == PieceColor.White ? PieceColor.Black : PieceColor.White;
        IsGamePaused = false;
        
        _legalGenerator.GetAllLegalMoves(_pieces, _colorToPlay);
        BoardState newState = new(this, _legalGenerator);
        newState.SetPawnBeforePromotion(oldPawn);
        newState.SetPromotedPiece(promotedPawn);
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
            {
                file += (int) char.GetNumericValue(symbol);
                continue;
            }
            int pieceColor = char.IsUpper(symbol) ? (int)PieceColor.White : (int)PieceColor.Black;
            int pieceType = pieceTypeFromSymbol[char.ToLower(symbol)];
            
            _board[rank, file] = pieceType + pieceColor;
            SpawnPiece(GetPieceData(pieceType, pieceColor), new Vector2Int(file, rank));
            file++;
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
        string msg = "";
        foreach (KeyValuePair<Vector2Int, Piece> piece in _pieces)
        {
            msg += $"{piece.Value} at {piece.Key}; ";
        }
        Debug.Log(msg);
    }
}