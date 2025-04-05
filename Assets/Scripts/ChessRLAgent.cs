using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
public class ChessRLAgent : Agent
{
    [SerializeField] ChessBoard chessBoard;
    [SerializeField] int team;
    private void Start()
    {
        // Initiate decision only if this agent's turn at startup.
        if (chessBoard.GetIsWhiteTurn() && team == 0 || !chessBoard.GetIsWhiteTurn() && team == 1)
        {
            RequestDecision();
        }
    }
    public override void OnEpisodeBegin()
    {
        //chessBoard.OnResetButton();
        //if (chessBoard.GetIsWhiteTurn() && team == 0 || !chessBoard.GetIsWhiteTurn() && team == 1)
        //{
        //    RequestDecision();
        //}
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(team);
        int[,] intArray = chessBoard.GetChessPieces();
        foreach (int i in intArray)
            sensor.AddObservation(i);
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        bool isWhiteTurn = chessBoard.GetIsWhiteTurn();
        if ((isWhiteTurn && team != 0) || (!isWhiteTurn && team != 1))
        {
            RequestDecision();
            return;
        }
        int pieceX = actions.DiscreteActions[0];
        int pieceY = actions.DiscreteActions[1];
        int targetX = actions.DiscreteActions[2];
        int targetY = actions.DiscreteActions[3];
        ChessPiece cp;
        chessBoard.GetPieceAt(out cp, pieceX, pieceY);
        if (cp == null)
        {
            AddReward(-0.01f);
            RequestDecision();
            return;
        }
        else
        {
            if (cp.team != team)
            {
                AddReward(-0.01f);
                RequestDecision();
                return;
            }
            else
            {
                AddReward(+0.02f);
            }
        }
        if(cp != null)
        {
            if (!chessBoard.MoveTo(cp, targetX, targetY))
            {
                AddReward(-0.1f);
                RequestDecision();
                return;
            }
            else
            {
                AddReward(0.1f);
                chessBoard.GetPieceAt(out cp, targetX, targetY);
                if(cp != null && cp.team != team)
                {
                    AddReward(0.1f * (int)cp.type);
                }
            }
        }
        switch (chessBoard.CheckForCheckmate())
        {
            default:
                break;
            case 1:
                if (isWhiteTurn)
                {
                    if (team == 0)
                        AddReward(-5.0f);
                    else if (team == 1)
                        AddReward(5.0f);
                }
                else
                {
                    if (team == 0)
                        AddReward(5.0f);
                    else if (team == 1)
                        AddReward(-5.0f);
                }
                EndEpisode();
                break;
            case 2:
            case 3:
            case 4:
                AddReward(1.0f);
                EndEpisode();
                break;
        }
    }
}
