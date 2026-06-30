using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class StockfishAI : MonoBehaviour
{
    [Header("Stockfish Settings")]
    [SerializeField] private ChessBoard chessBoard;
    [SerializeField] private int skillLevel = 20; // 0 (Easy) to 20 (Grandmaster)
    [SerializeField] private int searchDepth = 15;

    private Process stockfishProcess;
    private StreamWriter sfInput;
    private StreamReader sfOutput;
    private bool isThinking = false;

    private void Start()
    {
        StartStockfishProcess();
    }

    private void StartStockfishProcess()
    {
        string exeName = "stockfish-windows-x86-64-avx2.exe"; // Change to binary name if targeting Mac/Linux
        string path = Path.Combine(Application.streamingAssetsPath, exeName);

        if (!File.Exists(path))
        {
            Debug.LogError($"Stockfish not found at {path}");
            return;
        }

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        stockfishProcess = new Process { StartInfo = psi };
        stockfishProcess.Start();

        sfInput = stockfishProcess.StandardInput;
        sfOutput = stockfishProcess.StandardOutput;

        // Initialize UCI
        SendCommand("uci");
        SendCommand($"setoption name Skill Level value {skillLevel}");
        SendCommand("isready");
    }

    private void SendCommand(string command)
    {
        if (sfInput != null)
        {
            sfInput.WriteLine(command);
            sfInput.Flush();
        }
    }

    public async void PlayTurn()
    {
        if (isThinking) return;
        isThinking = true;

        // 1. Tell Stockfish the current board state based on the move history
        string moves = chessBoard.uciMoveHistory.Trim();
        if (string.IsNullOrEmpty(moves))
            SendCommand("position startpos");
        else
            SendCommand($"position startpos moves {moves}");

        // 2. Ask Stockfish to calculate the best move
        SendCommand($"go depth {searchDepth}");

        // 3. Wait for the result on a background thread so Unity doesn't freeze
        string bestMoveString = await Task.Run(() => GetBestMoveFromOutput());

        isThinking = false;

        // 4. Execute the move back on Unity's main thread
        if (!string.IsNullOrEmpty(bestMoveString))
        {
            ChessNotationHelper.FromUCI(bestMoveString, out int fromX, out int fromY, out int toX, out int toY);

            chessBoard.GetPieceAt(out ChessPiece pieceToMove, fromX, fromY);
            if (pieceToMove != null)
            {
                chessBoard.MoveTo(pieceToMove, toX, toY);
            }
        }
    }

    private string GetBestMoveFromOutput()
    {
        string line;
        while ((line = sfOutput.ReadLine()) != null)
        {
            // The engine will spit out a lot of evaluation data. 
            // We only care about the line that starts with "bestmove"
            if (line.StartsWith("bestmove"))
            {
                // Format is usually: "bestmove e2e4 ponder e7e5"
                string[] parts = line.Split(' ');
                if (parts.Length > 1)
                {
                    return parts[1]; // This is the "e2e4" part
                }
            }
        }
        return null;
    }

    private void OnDestroy()
    {
        if (stockfishProcess != null && !stockfishProcess.HasExited)
        {
            SendCommand("quit");
            stockfishProcess.Close();
        }
    }
}