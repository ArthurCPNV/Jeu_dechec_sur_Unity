using UnityEngine;
using UnityEngine.UI;

public enum PieceType
{
    None,
    WhitePawn,
    WhiteKnight,
    WhiteBishop,
    WhiteRook,
    WhiteQueen,
    WhiteKing,
    BlackPawn,
    BlackKnight,
    BlackBishop,
    BlackRook,
    BlackQueen,
    BlackKing
}

public class Board : MonoBehaviour
{
    [Header("TileColors")]
    public Color32 darkSquareColor = new Color32(171, 122, 101, 255);
    public Color32 lightSquareColor = new Color32(238, 216, 192, 255);

    [Header("BoardSize")]
    public static int boardSize = 8;

    [Header("Prefabs")]
    public GameObject tilePrefab;

    // Managers
    private GameManager _gameManager;

    // Tiles
    private Tile[,] _tiles;
    private Piece[,] _pieces;
    private const int XOffset = -450;
    private const int YOffset = -263;

    private static readonly PieceType[,] _initialBoardPosition = new PieceType[8, 8]
    {
        { PieceType.BlackRook, PieceType.BlackKnight, PieceType.BlackBishop, PieceType.BlackQueen, PieceType.BlackKing, PieceType.BlackBishop, PieceType.BlackKnight, PieceType.BlackRook },
        { PieceType.BlackPawn, PieceType.BlackPawn, PieceType.BlackPawn, PieceType.BlackPawn, PieceType.BlackPawn, PieceType.BlackPawn, PieceType.BlackPawn, PieceType.BlackPawn },
        { PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None },
        { PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None },
        { PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None },
        { PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None },
        { PieceType.WhitePawn, PieceType.WhitePawn, PieceType.WhitePawn, PieceType.WhitePawn, PieceType.WhitePawn, PieceType.WhitePawn, PieceType.WhitePawn, PieceType.WhitePawn },
        { PieceType.WhiteRook, PieceType.WhiteKnight, PieceType.WhiteBishop, PieceType.WhiteQueen, PieceType.WhiteKing, PieceType.WhiteBishop, PieceType.WhiteKnight, PieceType.WhiteRook }
    };

    private void Start()
    {
        _tiles = new Tile[boardSize, boardSize];
        _pieces = new Piece[boardSize, boardSize];

        _gameManager = GameManager.Instance;

        InitializeBoard();
    }

    /// <summary>
    /// Initialize the tiles of the board
    /// </summary>
    public void InitializeBoard()
    {
        // cache the size of the tile prefab
        RectTransform tilePrefabRectTransform = tilePrefab.GetComponent<RectTransform>();
        float tileWidth = tilePrefabRectTransform.sizeDelta.x;
        float tileHeight = tilePrefabRectTransform.sizeDelta.y;

        for (int rank = 0; rank < boardSize; rank++)
        {
            for (int file = 0; file < boardSize; file++)
            {
                // Get the position in which the tile should be placed
                Vector3 tilePosition = new Vector3(XOffset + (tileWidth * rank), YOffset + (tileHeight * file), 0);

                // Initialise the tile
                GameObject tileObject = Instantiate(tilePrefab, tilePosition, Quaternion.identity);
                tileObject.transform.SetParent(transform);
                tileObject.name = (char)('a' + rank) + (file + 1).ToString();

                // Change the color of the tile alternating from dark to light squares
                Image tileImage = tileObject.GetComponent<Image>();
                tileImage.color = ((file + rank) % 2 == 0) ? darkSquareColor : lightSquareColor;

                // Add tile to the tiles array
                Tile tileScript = tileObject.GetComponent<Tile>();
                _tiles[file, rank] = tileScript;
                tileScript.DefaultColor = tileImage.color;

                GameObject tilePieceObject = tileObject.transform.Find("Piece").gameObject;

                _pieces[file, rank] = InitializePiece(_initialBoardPosition[boardSize - 1 - file, rank], tilePieceObject);
            }
        }
    }

    /// <summary>
    /// Initialize the piece according to it's type
    /// </summary>
    /// <param name="pieceType">The type of the piece to initialize.</param>
    /// <param name="pieceObject">The GameObject representing the piece.</param>
    /// <returns></returns>
    private Piece InitializePiece(PieceType pieceType, GameObject pieceObject)
    {
        Tile pieceTile = pieceObject.transform.parent.gameObject.GetComponent<Tile>();

        if (pieceType == PieceType.None)
        {
            pieceTile.RemovePiece();
            return null;
        }

        Image pieceImage = pieceObject.GetComponent<Image>();
        SpriteManager.Instance.SetImageSprite(pieceType, pieceImage);

        if (pieceType != PieceType.None)
        {
            pieceTile.InitializePiece(pieceType);
        }
        
        Piece pieceScript = pieceObject.GetComponent<Piece>();

        pieceObject.SetActive(true);

        return pieceScript;
    }

    public void MovePiece(Tile departureTile, Tile destinationTile)
    {
        GameObject pieceObject = departureTile.transform.Find("Piece").gameObject;
        Piece pieceScript = departureTile.OccupyingPiece;
        PieceType pieceType = pieceScript.pieceType;

        if (pieceType == PieceType.WhitePawn || pieceType == PieceType.BlackPawn)
        {
            Pawn pawn = pieceScript as Pawn;

            pawn.SetPieceHasMoved();
        }

        destinationTile.RemovePiece();
        destinationTile.PlacePiece(pieceObject);
        departureTile.RemovePiece();

        pieceScript.ResetGeneratedMoves();
        _gameManager.SwitchTurn();
    }

    /// <summary>
    /// Checks if the specified file and rank are within the board limits.
    /// </summary>
    /// <param name="fileToMove">The file to move the piece to.</param>
    /// <param name="rankToMove">The rank to move the piece to.</param>
    /// <returns>True if the position is within the board limits, false otherwise.</returns>
    public static bool IsInBoardLimits(int fileToMove, int rankToMove)
    {
        bool isInBoardLimits = (fileToMove >= 0 && rankToMove >= 0 && fileToMove < boardSize && rankToMove < boardSize);

        return isInBoardLimits;
    }

    public Tile[,] GetTiles
    { 
        get { return  _tiles; }
    }

    public Piece[,] GetPieces
    {
        get { return _pieces; }
    }
}
