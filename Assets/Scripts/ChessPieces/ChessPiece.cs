using System.Collections.Generic;
using UnityEngine;

public enum ChessPieceType
{
    None,
    Pawn,
    Bishop,
    Rook,
    Knight,
    Queen,
    King
}

public class ChessPiece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;
    public float originalY;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;
    private void Awake()
    {
        originalY = transform.position.y;
    }

    private void Start()
    {
        if(team == 1)
            transform.Rotate(0, 0, 180);
    }
    private void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public virtual List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        return new List<Vector2Int>();
    }
    public virtual SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        return SpecialMove.None;
    }
    public virtual void SetPosition(Vector3 position, bool force=false)
    {
        desiredPosition = position;
        if (force)
            transform.localPosition = desiredPosition;
    }

    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force)
            transform.localScale = desiredScale;
    }

}
