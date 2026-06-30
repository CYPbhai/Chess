using UnityEngine;

public static class ChessNotationHelper
{
    // Converts array indices [4, 1] to [4, 3] into a string like "e2e4"
    public static string ToUCI(int fromX, int fromY, int toX, int toY, SpecialMove specialMove)
    {
        char fileFrom = (char)('a' + fromX);
        char rankFrom = (char)('1' + fromY);
        char fileTo = (char)('a' + toX);
        char rankTo = (char)('1' + toY);

        string promotion = "";
        // Defaulting to Queen promotion for simplicity
        if (specialMove == SpecialMove.Promotion) promotion = "q";

        return $"{fileFrom}{rankFrom}{fileTo}{rankTo}{promotion}";
    }

    public static void FromUCI(string uci, out int fromX, out int fromY, out int toX, out int toY)
    {
        fromX = uci[0] - 'a';
        fromY = uci[1] - '1';
        toX = uci[2] - 'a';
        toY = uci[3] - '1';
    }
}