using UnityEngine;
using System.Collections.Generic;
using TMPro;

public enum SpecialMove
{
    None,
    EnPassant,
    Castling,
    Promotion
}

public class ChessBoard : MonoBehaviour
{
    [SerializeField] private ChessRLAgent whiteAgent, blackAgent;
    [Header("UI")]
    [SerializeField] private GameObject victoryScreen;

    [Header("Art")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffset = 1.5f;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    // LOGIC
    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Dictionary<GameObject, Vector2Int> tileLookup;
    private Camera currentCamera;
    private Vector2Int currentHover = new Vector2Int(-1, -1);
    private Vector3 bounds;
    private bool isWhiteTurn;
    private int layerTile, layerHover, layerHighlight;
    private Mesh sharedTileMesh;
    private SpecialMove specialMove;
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();

    // Preallocated arrays and lists for simulation reuse
    private ChessPiece[,] simulationGrid = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

    private void Awake()
    {
        isWhiteTurn = true;
        layerTile = LayerMask.NameToLayer("Tile");
        layerHover = LayerMask.NameToLayer("Hover");
        layerHighlight = LayerMask.NameToLayer("Highlight");
        tileLookup = new Dictionary<GameObject, Vector2Int>();

        GenerateTileMesh();
        GenerateGrid(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnPieces();
        PositionPieces();
        if (GameManager.Instance.IsTwoPlayer)
        {
            whiteAgent.gameObject.SetActive(false);
            blackAgent.gameObject.SetActive(false);
        }
        else
        {
            if(GameManager.Instance.IsPlayerWhite)
            {
                whiteAgent.gameObject.SetActive(false);
                blackAgent.gameObject.SetActive(true);
            }
            else
            {
                transform.Rotate(0, 180, 0);
                whiteAgent.gameObject.SetActive(true);
                blackAgent.gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (currentCamera == null)
        {
            currentCamera = Camera.main;
            if (currentCamera == null)
            {
                Debug.LogWarning("No main camera found. Tag a camera as 'MainCamera'!");
                return;
            }
        }
        if (GameManager.Instance.IsTwoPlayer)
        {
            HandleInput();
        }
        else
        {
            if((GameManager.Instance.IsPlayerWhite && isWhiteTurn) || (!GameManager.Instance.IsPlayerWhite && !isWhiteTurn))
            {
                HandleInput();
            }
        }
    }


    private void HandleInput()
    {
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit info, 100, (1 << layerTile) | (1 << layerHover) | (1 << layerHighlight)))
        {
            if (tileLookup.TryGetValue(info.transform.gameObject, out Vector2Int hitPosition))
            {
                if (currentHover.x >= 0)
                    tiles[currentHover.x, currentHover.y].layer = ContainsValidMove(ref availableMoves, currentHover) ? layerHighlight : layerTile;
                currentHover = hitPosition;
                tiles[currentHover.x, currentHover.y].layer = layerHover;

                if (Input.GetMouseButtonDown(0))
                {
                    if (chessPieces[hitPosition.x, hitPosition.y] != null &&
                        ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn) ||
                         (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn)))
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);
                        PreventCheck(currentlyDragging);
                        HighlightTiles();
                    }
                }

                if (currentlyDragging != null && Input.GetMouseButtonUp(0))
                {
                    if (!MoveTo(currentlyDragging, hitPosition.x, hitPosition.y))
                        ResetPiecePosition();

                    currentlyDragging = null;
                    RemoveHighlightTiles();
                }
            }
        }
        else
        {
            if (currentHover.x >= 0)
            {
                tiles[currentHover.x, currentHover.y].layer = ContainsValidMove(ref availableMoves, currentHover) ? layerHighlight : layerTile;
                currentHover = new Vector2Int(-1, -1);
            }
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                ResetPiecePosition();
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        if (currentlyDragging != null)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * currentlyDragging.originalY);
            if (horizontalPlane.Raycast(ray, out float distance))
            {
                Vector3 targetPosition = ray.GetPoint(distance);
                targetPosition.y = dragOffset;
                if(!GameManager.Instance.IsTwoPlayer && !GameManager.Instance.IsPlayerWhite)
                    targetPosition = transform.InverseTransformPoint(targetPosition);
                currentlyDragging.SetPosition(targetPosition);
            }
        }
    }

    private void ResetPiecePosition()
    {
        currentlyDragging.SetPosition(new Vector3(
            currentlyDragging.currentX * tileSize + tileSize / 2,
            currentlyDragging.originalY,
            currentlyDragging.currentY * tileSize + tileSize / 2) - bounds);
    }

    // Generate Board
    private void GenerateGrid(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountY / 2) * tileSize) + boardCenter;
        tiles = new GameObject[tileCountX, tileCountY];

        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                GameObject tile = GenerateTile(tileSize, x, y);
                tiles[x, y] = tile;
                tileLookup[tile] = new Vector2Int(x, y);
            }
        }
    }

    private GameObject GenerateTile(float tileSize, int x, int y)
    {
        GameObject tile = new GameObject($"Tile {x},{y}");
        tile.transform.parent = transform;
        MeshFilter meshFilter = tile.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = tile.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = sharedTileMesh;
        meshRenderer.material = tileMaterial;
        tile.transform.localPosition = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        tile.layer = layerTile;
        tile.AddComponent<BoxCollider>();
        return tile;
    }

    private void GenerateTileMesh()
    {
        sharedTileMesh = new Mesh();
        Vector3[] vertices = new Vector3[] {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, tileSize),
            new Vector3(tileSize, 0, 0),
            new Vector3(tileSize, 0, tileSize)
        };
        int[] triangles = { 0, 1, 2, 1, 3, 2 };
        sharedTileMesh.vertices = vertices;
        sharedTileMesh.triangles = triangles;
        sharedTileMesh.RecalculateNormals();
    }

    // Spawning of the pieces
    private void SpawnPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
        int whiteTeam = 0, blackTeam = 1;

        // White team
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);

        for (int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);

        // Black team
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);

        for (int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();
        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];
        return cp;
    }

    // Positioning
    private void PositionPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        if (chessPieces[x, y] == null) return;
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(new Vector3(x * tileSize + tileSize / 2, chessPieces[x, y].originalY, y * tileSize + tileSize / 2) - bounds, force);
    }

    // Highlight Tiles
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = layerHighlight;
    }

    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = layerTile;
        availableMoves.Clear();
    }

    // Checkmate
    private void CheckMate(int team)
    {
        DisplayVictory(team);
    }

    public void OnRestartButton()
    {
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();

        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                    Destroy(chessPieces[x, y].gameObject);
                chessPieces[x, y] = null;
            }
        }
        foreach (var cp in deadWhites)
            Destroy(cp.gameObject);
        foreach (var cp in deadBlacks)
            Destroy(cp.gameObject);
        deadWhites.Clear();
        deadBlacks.Clear();

        SpawnPieces();
        PositionPieces();
        isWhiteTurn = true;
    }

    public void OnMainMenuButton()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void DisplayVictory(int winningTeam)
    {
        VictoryScreenMenuUI.Instance.Show(GameplayMenuUI.Instance);
        GameplayMenuUI.Instance.Hide(false);
        switch (winningTeam)
        {
            case 0:
                VictoryScreenMenuUI.Instance.UpdateVictoryText("WHITE TEAM WON");
                break;
            case 1:
                VictoryScreenMenuUI.Instance.UpdateVictoryText("BLACK TEAM WON");
                break;
            case 2:
                VictoryScreenMenuUI.Instance.UpdateVictoryText("DRAW BY STALEMATE");
                break;
            case 3:
                VictoryScreenMenuUI.Instance.UpdateVictoryText("DRAW BY INSUFFICIENT MATERIAL");
                break;
            case 4:
                VictoryScreenMenuUI.Instance.UpdateVictoryText("DRAW BY 50 KING MOVES RULE");
                break;
        }
    }

    // Special Moves and Simulation
    private void ProcessSpecialMove()
    {
        if (specialMove == SpecialMove.EnPassant)
        {
            var newMove = moveList[moveList.Count - 1];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPosition = moveList[moveList.Count - 2];
            ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];

            if (myPawn.currentX == enemyPawn.currentX)
            {
                if (myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1)
                {
                    if (enemyPawn.team == 0)
                    {
                        deadWhites.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3(8 * tileSize, enemyPawn.originalY * deathSize, -tileSize / 2) - bounds +
                                               new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.forward * deathSpacing) * deadWhites.Count);
                    }
                    else
                    {
                        deadBlacks.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3(-1 * tileSize, enemyPawn.originalY * deathSize, 7.5f * tileSize) - bounds +
                                               new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.back * deathSpacing) * deadBlacks.Count);
                    }
                    chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
                }
            }
        }

        if (specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];
            if (targetPawn.type == ChessPieceType.Pawn)
            {
                if (targetPawn.team == 0 && lastMove[1].y == 7)
                {
                    ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 0);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                }
                if (targetPawn.team == 1 && lastMove[1].y == 0)
                {
                    ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 1);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                }
            }
        }

        if (specialMove == SpecialMove.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            if (lastMove[1].x == 2)
            {
                if (lastMove[1].y == 0)
                {
                    chessPieces[3, 0] = chessPieces[0, 0];
                    PositionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;
                }
                else if (lastMove[1].y == 7)
                {
                    chessPieces[3, 7] = chessPieces[0, 7];
                    PositionSinglePiece(3, 7);
                    chessPieces[0, 7] = null;
                }
            }
            else if (lastMove[1].x == 6)
            {
                if (lastMove[1].y == 0)
                {
                    chessPieces[5, 0] = chessPieces[7, 0];
                    PositionSinglePiece(5, 0);
                    chessPieces[7, 0] = null;
                }
                else if (lastMove[1].y == 7)
                {
                    chessPieces[5, 7] = chessPieces[7, 7];
                    PositionSinglePiece(5, 7);
                    chessPieces[7, 7] = null;
                }
            }
        }
    }

    private void PreventCheck(ChessPiece cp)
    {
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] == null) continue;
                if (chessPieces[x, y].type == ChessPieceType.King && chessPieces[x, y].team == cp.team)
                {
                    targetKing = chessPieces[x, y];
                    break;
                }
            }
            if (targetKing != null) break;
        }
        if (targetKing != null)
        {
            SimulateMoveForSinglePiece(cp, ref availableMoves, targetKing);
        }
    }

    private void SimulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
    {
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                simulationGrid[x, y] = chessPieces[x, y];
            }
        }

        foreach (Vector2Int move in moves)
        {
            int simX = move.x;
            int simY = move.y;
            Vector2Int kingPosSim = (cp.type == ChessPieceType.King) ? new Vector2Int(simX, simY) : new Vector2Int(targetKing.currentX, targetKing.currentY);

            // Simulate the move
            simulationGrid[actualX, actualY] = null;
            int oldX = cp.currentX, oldY = cp.currentY;
            cp.currentX = simX;
            cp.currentY = simY;
            simulationGrid[simX, simY] = cp;

            // Check if any enemy piece can attack the king's position
            List<Vector2Int> simAttacks = new List<Vector2Int>();
            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    ChessPiece piece = simulationGrid[x, y];
                    if (piece != null && piece.team != cp.team)
                    {
                        List<Vector2Int> pieceMoves = piece.GetAvailableMoves(ref simulationGrid, TILE_COUNT_X, TILE_COUNT_Y);
                        simAttacks.AddRange(pieceMoves);
                    }
                }
            }
            if (ContainsValidMove(ref simAttacks, kingPosSim))
            {
                movesToRemove.Add(move);
            }
            cp.currentX = oldX;
            cp.currentY = oldY;
            simulationGrid[actualX, actualY] = cp;
            simulationGrid[simX, simY] = chessPieces[simX, simY];
        }

        foreach (Vector2Int invalidMove in movesToRemove)
        {
            moves.Remove(invalidMove);
        }
    }

    public int CheckForCheckmate()
    {
        if (moveList.Count == 0)
        {
            // No moves have been made; return 0 indicating the game continues.
            return 0;
        }
        var lastMove = moveList[moveList.Count - 1];
        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;
        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;

        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] == null) continue;
                if (chessPieces[x, y].team == targetTeam)
                {
                    defendingPieces.Add(chessPieces[x, y]);
                    if (chessPieces[x, y].type == ChessPieceType.King)
                        targetKing = chessPieces[x, y];
                }
                else
                {
                    attackingPieces.Add(chessPieces[x, y]);
                }
            }
        }

        // Is the king attacked right now?
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            List<Vector2Int> pieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            currentAvailableMoves.AddRange(pieceMoves);
        }

        // Is the King in check?
        if (ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY)))
        {
            // King is under attack, can we move something to help him?
            foreach (var cp in defendingPieces)
            {
                List<Vector2Int> defendingMoves = cp.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                SimulateMoveForSinglePiece(cp, ref defendingMoves, targetKing);
                if (defendingMoves.Count != 0)
                {
                    AudioManager.Instance.PlaySFX("Check");
                    return 0; // There is a valid move, no checkmate
                }
            }
            AudioManager.Instance.PlaySFX("Checkmate");
            return 1; // Checkmate Exit
        }
        else
        {
            // Stalemate Check: If no valid move exists
            bool hasLegalMove = false;
            foreach (var cp in defendingPieces)
            {
                List<Vector2Int> defendingMoves = cp.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                SimulateMoveForSinglePiece(cp, ref defendingMoves, targetKing);
                if (defendingMoves.Count != 0)
                {
                    hasLegalMove = true;
                    break;
                }
            }
            if (!hasLegalMove)
            {
                AudioManager.Instance.PlaySFX("Checkmate");
                return 2; // Stalemate Exit
            }

            // Insufficient Material Check
            if (IsInsufficientMaterial()) 
            {
                AudioManager.Instance.PlaySFX("Checkmate");
                return 3; 
            } // Draw by insufficient material

            // 50-Move Rule Check (Removed reference to a non-existent move[2])
            if (IsFiftyMoveRule())
            {
                AudioManager.Instance.PlaySFX("Checkmate");
                return 4; // Draw by 50-move rule
            }
        }
        return 0; // Game continues
    }

    private bool IsInsufficientMaterial()
    {
        List<ChessPiece> remainingPieces = new List<ChessPiece>();
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                    remainingPieces.Add(chessPieces[x, y]);

        // Only kings left
        if (remainingPieces.Count == 2) return true;

        // King + Knight vs King or King + Bishop vs King
        if (remainingPieces.Count == 3)
        {
            foreach (var piece in remainingPieces)
            {
                if (piece.type == ChessPieceType.Knight || piece.type == ChessPieceType.Bishop)
                    return true;
            }
        }
        return false; // Otherwise, it's not insufficient material
    }

    private bool IsFiftyMoveRule()
    {
        int moveCount = 0;
        for (int i = moveList.Count - 1; i >= 0; i--)
        {
            var move = moveList[i];
            ChessPiece movedPiece = chessPieces[move[1].x, move[1].y];

            // Check for pawn moves or captures
            if (movedPiece != null && (movedPiece.type == ChessPieceType.Pawn || move[1] != move[0]))
            {
                moveCount = 0; // Reset count due to pawn move or capture
            }
            else
            {
                moveCount++;
            }

            // Check if 50-move rule is satisfied
            if (moveCount >= 50)
            {
                return true;
            }
        }
        return false;
    }

    // Operations
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2Int pos)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;
        }
        return false;
    }

    public bool MoveTo(ChessPiece cp, int x, int y)
    {
        availableMoves = cp.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
        specialMove = cp.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);
        PreventCheck(cp);
        if (!ContainsValidMove(ref availableMoves, new Vector2Int(x, y)))
            return false;
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];
            if (cp.team == ocp.team)
                return false;
            AudioManager.Instance.PlaySFX("Capture");
            if (ocp.team == 0)
            {
                if (ocp.type == ChessPieceType.King)
                    CheckMate(1);
                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(8 * tileSize, ocp.originalY * deathSize, -tileSize / 2) - bounds +
                                new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else
            {
                if (ocp.type == ChessPieceType.King)
                    CheckMate(0);
                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(-1 * tileSize, ocp.originalY * deathSize, 7.5f * tileSize) - bounds +
                                new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.back * deathSpacing) * deadBlacks.Count);
            }
        }
        else
        {
            AudioManager.Instance.PlaySFX("Move");
        }
        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;
        PositionSinglePiece(x, y);
        isWhiteTurn = !isWhiteTurn;
        GameplayMenuUI.Instance.UpdateTurnText(isWhiteTurn);
        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });
        ProcessSpecialMove();
        switch (CheckForCheckmate())
        {
            default:
                break;
            case 1:
                CheckMate(cp.team);
                break;
            case 2:
                CheckMate(2);
                break;
            case 3:
                CheckMate(3);
                break;
            case 4:
                CheckMate(4);
                break;
        }
        if (!GameManager.Instance.IsTwoPlayer)
        {
            if (isWhiteTurn && !GameManager.Instance.IsPlayerWhite)
                whiteAgent.RequestDecision();
            else if(!isWhiteTurn && GameManager.Instance.IsPlayerWhite)
                blackAgent.RequestDecision();
            RemoveHighlightTiles();
        }
        return true;
    }

    public bool GetIsWhiteTurn()
    {
        return isWhiteTurn;
    }

    // Returns the ChessPiece at a given position.
    public void GetPieceAt(out ChessPiece cp, int x, int y)
    {
        cp = chessPieces[x, y];
    }

    public int[,] GetChessPieces()
    {
        int[,] intArray = new int[TILE_COUNT_X, TILE_COUNT_Y];
        for(int x=0; x< TILE_COUNT_X; x++)
        {
            for(int y=0; y<TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] == null)
                {
                    intArray[x, y] = 0;
                }
                else
                    intArray[x, y] = chessPieces[x, y].team == 0 ? (int)chessPieces[x, y].type : -(int)chessPieces[x, y].type;
            }
        }
        return intArray;
    }
}