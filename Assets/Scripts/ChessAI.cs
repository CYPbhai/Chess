using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChessAI : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private ChessBoard chessBoard;
    [SerializeField] private int team; // 0 = White, 1 = Black
    private int searchDepth = 4;
    private bool isThinking = false;
    private System.Random rng = new System.Random(); // Class-level RNG for variance

    // Tracks the string representations of the last few board states in the actual game
    private List<string> actualGameHistory = new List<string>();

    private struct VirtualPiece
    {
        public ChessPieceType type;
        public int team;
        public int x;
        public int y;
    }

    private struct VirtualMove
    {
        public int fromX;
        public int fromY;
        public int toX;
        public int toY;
    }

    public struct AIMove
    {
        public int startX, startY;
        public int targetX, targetY;
        public ChessPiece piece;
        public ChessPiece capturedPiece;
    }

    private void Start()
    {
        switch (GameManager.Instance.Difficulty)
        {
            case AIDifficulty.Easy:
                searchDepth = 3;
                break;
            case AIDifficulty.Medium:
                searchDepth = 4;
                break;
            case AIDifficulty.Hard:
                searchDepth = 5;
                break;
        }
    }

    public void PlayTurn()
    {
        if (!isThinking)
        {
            ExecuteAIMove();
        }
    }


    // Generates a quick, unique string key representing the current board layout
    private string GetBoardStateKey(VirtualPiece[,] board)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                sb.Append((int)board[x, y].type);
                sb.Append(board[x, y].team);
            }
        }
        return sb.ToString();
    }

    private async void ExecuteAIMove()
    {
        isThinking = true;
        VirtualPiece[,] boardSnapshot = CreateBoardSnapshot();

        // Save current state to history
        string currentKey = GetBoardStateKey(boardSnapshot);
        actualGameHistory.Add(currentKey);

        // Keep history lean (e.g., last 8 states is plenty to catch loops)
        if (actualGameHistory.Count > 16) actualGameHistory.RemoveAt(0);

        VirtualMove bestMove = await Task.Run(() => CalculateBestMoveBackground(boardSnapshot, searchDepth, team));

        if (bestMove.fromX != -1)
        {
            chessBoard.GetPieceAt(out ChessPiece pieceToMove, bestMove.fromX, bestMove.fromY);
            if (pieceToMove != null)
            {
                chessBoard.MoveTo(pieceToMove, bestMove.toX, bestMove.toY);
            }
        }

        isThinking = false;
    }

    private VirtualPiece[,] CreateBoardSnapshot()
    {
        VirtualPiece[,] snapshot = new VirtualPiece[8, 8];

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                chessBoard.GetPieceAt(out ChessPiece livePiece, x, y);

                if (livePiece != null)
                {
                    snapshot[x, y] = new VirtualPiece
                    {
                        type = livePiece.type,
                        team = livePiece.team,
                        x = x,
                        y = y
                    };
                }
                else
                {
                    snapshot[x, y] = new VirtualPiece { type = ChessPieceType.None, team = -1, x = x, y = y };
                }
            }
        }
        return snapshot;
    }

    private VirtualMove CalculateBestMoveBackground(VirtualPiece[,] board, int depth, int activeTeam)
    {
        List<VirtualMove> legalMoves = GenerateLegalMoves(board, activeTeam);

        if (legalMoves.Count == 0)
            return new VirtualMove { fromX = -1 };

        // Fisher-Yates shuffle to give equal moves organic variance
        for (int i = legalMoves.Count - 1; i > 0; i--)
        {
            int k = rng.Next(i + 1);
            VirtualMove temp = legalMoves[k];
            legalMoves[k] = legalMoves[i];
            legalMoves[i] = temp;
        }

        VirtualMove bestMove = legalMoves[0];
        int bestValue = int.MinValue;
        int alpha = int.MinValue;
        int beta = int.MaxValue;

        foreach (VirtualMove move in legalMoves)
        {
            // Simulate Virtual Move
            VirtualPiece targetCellBackup = board[move.toX, move.toY];
            VirtualPiece movingPieceBackup = board[move.fromX, move.fromY];

            board[move.toX, move.toY] = movingPieceBackup;
            board[move.toX, move.toY].x = move.toX;
            board[move.toX, move.toY].y = move.toY;
            board[move.fromX, move.fromY] = new VirtualPiece { type = ChessPieceType.None, team = -1 };

            int score = Minimax(board, depth - 1, alpha, beta, false, activeTeam);

            // Undo Virtual Move
            board[move.fromX, move.fromY] = movingPieceBackup;
            board[move.fromX, move.fromY].x = move.fromX;
            board[move.fromX, move.fromY].y = move.fromY;
            board[move.toX, move.toY] = targetCellBackup;

            if (score > bestValue)
            {
                bestValue = score;
                bestMove = move;
            }
            alpha = Math.Max(alpha, bestValue);
        }

        return bestMove;
    }

    private int Minimax(VirtualPiece[,] board, int depth, int alpha, int beta, bool isMaximizing, int activeTeam)
    {
        if (depth == 0)
        {
            return EvaluateBoard(board, activeTeam);
        }

        int turnTeam = isMaximizing ? activeTeam : (1 - activeTeam);

        // Minimax branches should only traverse strictly legal chess moves
        List<VirtualMove> moves = GenerateLegalMoves(board, turnTeam);

        if (moves.Count == 0)
        {
            return isMaximizing ? -100000 - depth : 100000 + depth;
        }

        // Custom sorting heuristic using .CompareTo() 
        moves.Sort((a, b) => ScoreMoveForSorting(board, b).CompareTo(ScoreMoveForSorting(board, a)));

        if (isMaximizing)
        {
            int maxEval = int.MinValue;
            foreach (VirtualMove move in moves)
            {
                VirtualPiece targetBackup = board[move.toX, move.toY];
                VirtualPiece moverBackup = board[move.fromX, move.fromY];

                board[move.toX, move.toY] = moverBackup;
                board[move.toX, move.toY].x = move.toX;
                board[move.toX, move.toY].y = move.toY;
                board[move.fromX, move.fromY] = new VirtualPiece { type = ChessPieceType.None, team = -1 };

                int eval = Minimax(board, depth - 1, alpha, beta, false, activeTeam);

                board[move.fromX, move.fromY] = moverBackup;
                board[move.fromX, move.fromY].x = move.fromX;
                board[move.fromX, move.fromY].y = move.fromY;
                board[move.toX, move.toY] = targetBackup;

                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha) break;
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (VirtualMove move in moves)
            {
                VirtualPiece targetBackup = board[move.toX, move.toY];
                VirtualPiece moverBackup = board[move.fromX, move.fromY];

                board[move.toX, move.toY] = moverBackup;
                board[move.toX, move.toY].x = move.toX;
                board[move.toX, move.toY].y = move.toY;
                board[move.fromX, move.fromY] = new VirtualPiece { type = ChessPieceType.None, team = -1 };

                int eval = Minimax(board, depth - 1, alpha, beta, true, activeTeam);

                board[move.fromX, move.fromY] = moverBackup;
                board[move.fromX, move.fromY].x = move.fromX;
                board[move.fromX, move.fromY].y = moverBackup.y;
                board[move.toX, move.toY] = targetBackup;

                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                if (beta <= alpha) break;
            }
            return minEval;
        }
    }

    // MVV-LVA calculation for optimized move sorting without breaking transitivity
    private int ScoreMoveForSorting(VirtualPiece[,] board, VirtualMove move)
    {
        VirtualPiece victim = board[move.toX, move.toY];
        VirtualPiece attacker = board[move.fromX, move.fromY];

        if (victim.type != ChessPieceType.None)
        {
            // Capturing a highly valuable piece with a low-value piece scores highest
            return (GetPieceWeight(victim.type) * 10) - GetPieceWeight(attacker.type);
        }
        return 0;
    }

    // Simulates all potential moves and strips away any that leave the King vulnerable
    private List<VirtualMove> GenerateLegalMoves(VirtualPiece[,] board, int checkTeam)
    {
        List<VirtualMove> allRawMoves = GenerateAllMoves(board, checkTeam);
        List<VirtualMove> legalMoves = new List<VirtualMove>();

        foreach (VirtualMove move in allRawMoves)
        {
            VirtualPiece targetBackup = board[move.toX, move.toY];
            VirtualPiece moverBackup = board[move.fromX, move.fromY];

            // Perform virtual simulation step
            board[move.toX, move.toY] = moverBackup;
            board[move.toX, move.toY].x = move.toX;
            board[move.toX, move.toY].y = move.toY;
            board[move.fromX, move.fromY] = new VirtualPiece { type = ChessPieceType.None, team = -1 };

            // Only retain this move if our King is completely safe after it executes
            if (!IsKingInCheck(board, checkTeam))
            {
                legalMoves.Add(move);
            }

            // Reverse simulation step
            board[move.fromX, move.fromY] = moverBackup;
            board[move.fromX, move.fromY].x = move.fromX;
            board[move.fromX, move.fromY].y = move.fromY;
            board[move.toX, move.toY] = targetBackup;
        }

        return legalMoves;
    }

    // Scans the board to determine if an opponent piece can hit your King's coordinates
    private bool IsKingInCheck(VirtualPiece[,] board, int checkTeam)
    {
        int kingX = -1, kingY = -1;

        // Locate King
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y].type == ChessPieceType.King && board[x, y].team == checkTeam)
                {
                    kingX = x;
                    kingY = y;
                    break;
                }
            }
            if (kingX != -1) break;
        }

        if (kingX == -1) return false; // Safety fallback

        // Gather geometric target paths for the enemy team
        int enemyTeam = 1 - checkTeam;
        List<VirtualMove> enemyMoves = new List<VirtualMove>();
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y].team == enemyTeam)
                {
                    CalculatePieceMoves(board, x, y, enemyMoves);
                }
            }
        }

        // Verify if any enemy paths converge onto our King
        foreach (VirtualMove enemyMove in enemyMoves)
        {
            if (enemyMove.toX == kingX && enemyMove.toY == kingY)
            {
                return true;
            }
        }

        return false;
    }

    private int EvaluateBoard(VirtualPiece[,] board, int activeTeam)
    {
        int totalScore = 0; 
        string currentSimulatedState = GetBoardStateKey(board);
        if (actualGameHistory.Contains(currentSimulatedState))
        {
            // Heavily penalize repeating positions so the AI looks for alternatives
            totalScore -= 500;
        }
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                VirtualPiece p = board[x, y];
                if (p.type != ChessPieceType.None)
                {
                    int weight = GetPieceWeight(p.type);

                    if ((x == 3 || x == 4) && (y == 3 || y == 4))
                        weight += 2;

                    if (p.team == activeTeam) totalScore += weight;
                    else totalScore -= weight;
                }
            }
        }
        return totalScore;
    }

    private int GetPieceWeight(ChessPieceType type)
    {
        switch (type)
        {
            case ChessPieceType.Pawn: return 10;
            case ChessPieceType.Knight: return 32;
            case ChessPieceType.Bishop: return 33;
            case ChessPieceType.Rook: return 50;
            case ChessPieceType.Queen: return 90;
            case ChessPieceType.King: return 900000;
            default: return 0;
        }
    }

    private List<VirtualMove> GenerateAllMoves(VirtualPiece[,] board, int checkTeam)
    {
        List<VirtualMove> moves = new List<VirtualMove>();
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y].team == checkTeam)
                {
                    CalculatePieceMoves(board, x, y, moves);
                }
            }
        }
        return moves;
    }

    private void CalculatePieceMoves(VirtualPiece[,] board, int cx, int cy, List<VirtualMove> moves)
    {
        VirtualPiece p = board[cx, cy];
        int team = p.team;

        switch (p.type)
        {
            case ChessPieceType.Pawn:
                int direction = (team == 0) ? 1 : -1;
                if (cy + direction >= 0 && cy + direction < 8 && board[cx, cy + direction].type == ChessPieceType.None)
                {
                    moves.Add(new VirtualMove { fromX = cx, fromY = cy, toX = cx, toY = cy + direction });
                    int startRow = (team == 0) ? 1 : 6;
                    if (cy == startRow && board[cx, cy + (direction * 2)].type == ChessPieceType.None)
                    {
                        moves.Add(new VirtualMove { fromX = cx, fromY = cy, toX = cx, toY = cy + (direction * 2) });
                    }
                }
                if (cx - 1 >= 0 && cy + direction >= 0 && cy + direction < 8 && board[cx - 1, cy + direction].type != ChessPieceType.None && board[cx - 1, cy + direction].team != team)
                    moves.Add(new VirtualMove { fromX = cx, fromY = cy, toX = cx - 1, toY = cy + direction });
                if (cx + 1 < 8 && cy + direction >= 0 && cy + direction < 8 && board[cx + 1, cy + direction].type != ChessPieceType.None && board[cx + 1, cy + direction].team != team)
                    moves.Add(new VirtualMove { fromX = cx, fromY = cy, toX = cx + 1, toY = cy + direction });
                break;

            case ChessPieceType.Knight:
                int[] kX = { 1, 2, 2, 1, -1, -2, -2, -1 };
                int[] kY = { 2, 1, -1, -2, -2, -1, 1, 2 };
                for (int i = 0; i < 8; i++)
                {
                    int tx = cx + kX[i];
                    int ty = cy + kY[i];
                    if (tx >= 0 && tx < 8 && ty >= 0 && ty < 8)
                    {
                        if (board[tx, ty].type == ChessPieceType.None || board[tx, ty].team != team)
                            moves.Add(new VirtualMove { fromX = cx, fromY = cy, toX = tx, toY = ty });
                    }
                }
                break;

            case ChessPieceType.Bishop:
                TraverseSliding(board, cx, cy, 1, 1, moves, team);
                TraverseSliding(board, cx, cy, 1, -1, moves, team);
                TraverseSliding(board, cx, cy, -1, 1, moves, team);
                TraverseSliding(board, cx, cy, -1, -1, moves, team);
                break;

            case ChessPieceType.Rook:
                TraverseSliding(board, cx, cy, 1, 0, moves, team);
                TraverseSliding(board, cx, cy, -1, 0, moves, team);
                TraverseSliding(board, cx, cy, 0, 1, moves, team);
                TraverseSliding(board, cx, cy, 0, -1, moves, team);
                break;

            case ChessPieceType.Queen:
                TraverseSliding(board, cx, cy, 1, 1, moves, team);
                TraverseSliding(board, cx, cy, 1, -1, moves, team);
                TraverseSliding(board, cx, cy, -1, 1, moves, team);
                TraverseSliding(board, cx, cy, -1, -1, moves, team);
                TraverseSliding(board, cx, cy, 1, 0, moves, team);
                TraverseSliding(board, cx, cy, -1, 0, moves, team);
                TraverseSliding(board, cx, cy, 0, 1, moves, team);
                TraverseSliding(board, cx, cy, 0, -1, moves, team);
                break;

            case ChessPieceType.King:
                for (int xMod = -1; xMod <= 1; xMod++)
                {
                    for (int yMod = -1; yMod <= 1; yMod++)
                    {
                        if (xMod == 0 && yMod == 0) continue;
                        int tx = cx + xMod;
                        int ty = cy + yMod;
                        if (tx >= 0 && tx < 8 && ty >= 0 && ty < 8)
                        {
                            if (board[tx, ty].type == ChessPieceType.None || board[tx, ty].team != team)
                                moves.Add(new VirtualMove { fromX = cx, fromY = cy, toX = tx, toY = ty });
                        }
                    }
                }
                break;
        }
    }

    private void TraverseSliding(VirtualPiece[,] board, int sX, int sY, int dX, int dY, List<VirtualMove> moves, int team)
    {
        int tx = sX + dX;
        int ty = sY + dY;
        while (tx >= 0 && tx < 8 && ty >= 0 && ty < 8)
        {
            if (board[tx, ty].type == ChessPieceType.None)
            {
                moves.Add(new VirtualMove { fromX = sX, fromY = sY, toX = tx, toY = ty });
            }
            else
            {
                if (board[tx, ty].team != team)
                {
                    moves.Add(new VirtualMove { fromX = sX, fromY = sY, toX = tx, toY = ty });
                }
                break;
            }
            tx += dX;
            ty += dY;
        }
    }
}