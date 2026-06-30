using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        // Right - top right - bottom right
        if (currentX + 1 < tileCountX)
        {
            if (board[currentX + 1, currentY] == null || board[currentX + 1, currentY].team != team)
                r.Add(new Vector2Int(currentX + 1, currentY));
            if (currentY + 1 < tileCountY)
                if (board[currentX + 1, currentY + 1] == null || board[currentX + 1, currentY + 1].team != team)
                    r.Add(new Vector2Int(currentX + 1, currentY + 1));
            if (currentY - 1 >= 0)
                if (board[currentX + 1, currentY - 1] == null || board[currentX + 1, currentY - 1].team != team)
                    r.Add(new Vector2Int(currentX + 1, currentY - 1));
        }
        // Left - top left - bottom left
        if (currentX - 1 >= 0)
        {
            if (board[currentX - 1, currentY] == null || board[currentX - 1, currentY].team != team)
                r.Add(new Vector2Int(currentX - 1, currentY));
            if (currentY + 1 < tileCountY)
                if (board[currentX - 1, currentY + 1] == null || board[currentX - 1, currentY + 1].team != team)
                    r.Add(new Vector2Int(currentX - 1, currentY + 1));
            if (currentY - 1 >= 0)
                if (board[currentX - 1, currentY - 1] == null || board[currentX - 1, currentY - 1].team != team)
                    r.Add(new Vector2Int(currentX - 1, currentY - 1));
        }
        // Up
        if (currentY + 1 < tileCountY)
            if (board[currentX, currentY + 1] == null || board[currentX, currentY + 1].team != team)
                r.Add(new Vector2Int(currentX, currentY + 1));
        // Down
        if (currentY - 1 >= 0)
            if (board[currentX, currentY - 1] == null || board[currentX, currentY - 1].team != team)
                r.Add(new Vector2Int(currentX, currentY - 1));

        return r;
    }

    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        SpecialMove r = SpecialMove.None;
        int y = (team == 0) ? 0 : 7;

        // RULE 1: You cannot castle if the King is currently in check
        if (IsSquareUnderAttack(board, new Vector2Int(4, y), team))
            return SpecialMove.None;

        var kingMove = moveList.Find(m => m[0].x == 4 && m[0].y == y);
        var leftRook = moveList.Find(m => m[0].x == 0 && m[0].y == y);
        var rightRook = moveList.Find(m => m[0].x == 7 && m[0].y == y);

        if (kingMove == null && currentX == 4)
        {
            // --- QUEENSIDE CASTLING (Left Rook) ---
            if (leftRook == null)
            {
                if (board[0, y] != null && board[0, y].type == ChessPieceType.Rook && board[0, y].team == team)
                {
                    if (board[3, y] == null && board[2, y] == null && board[1, y] == null)
                    {
                        // RULE 2: The passing square (d1/d8) cannot be under attack
                        if (!IsSquareUnderAttack(board, new Vector2Int(3, y), team))
                        {
                            availableMoves.Add(new Vector2Int(2, y));
                            r = SpecialMove.Castling;
                        }
                    }
                }
            }

            // --- KINGSIDE CASTLING (Right Rook) ---
            if (rightRook == null)
            {
                if (board[7, y] != null && board[7, y].type == ChessPieceType.Rook && board[7, y].team == team)
                {
                    if (board[6, y] == null && board[5, y] == null)
                    {
                        // RULE 2: The passing square (f1/f8) cannot be under attack
                        if (!IsSquareUnderAttack(board, new Vector2Int(5, y), team))
                        {
                            availableMoves.Add(new Vector2Int(6, y));
                            r = SpecialMove.Castling;
                        }
                    }
                }
            }
        }

        return r;
    }

    // Helper method to determine if an enemy piece can strike a specific coordinate
    private bool IsSquareUnderAttack(ChessPiece[,] board, Vector2Int targetSquare, int myTeam)
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece piece = board[x, y];

                if (piece != null && piece.team != myTeam)
                {
                    // --- THE MISSING FIX: Manual override for pawns ---
                    if (piece.type == ChessPieceType.Pawn)
                    {
                        int enemyDirection = (piece.team == 0) ? 1 : -1;
                        if ((piece.currentX - 1 == targetSquare.x || piece.currentX + 1 == targetSquare.x) &&
                            piece.currentY + enemyDirection == targetSquare.y)
                        {
                            return true;
                        }
                        continue;
                    }

                    // Standard validation for all other pieces
                    List<Vector2Int> enemyMoves = piece.GetAvailableMoves(ref board, 8, 8);
                    if (enemyMoves.Exists(m => m.x == targetSquare.x && m.y == targetSquare.y))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}