using System;

namespace ChessEngine.ChessEngine;

public static class BoardRepresentation
{
    #region Squares

    public const int a8 = 0;
    public const int b8 = 1;
    public const int c8 = 2;
    public const int d8 = 3;
    public const int e8 = 4;
    public const int f8 = 5;
    public const int g8 = 6;
    public const int h8 = 7;

    public const int a7 = 8;
    public const int b7 = 9;
    public const int c7 = 10;
    public const int d7 = 11;
    public const int e7 = 12;
    public const int f7 = 13;
    public const int g7 = 14;
    public const int h7 = 15;

    public const int a6 = 16;
    public const int b6 = 1;
    public const int c6 = 18;
    public const int d6 = 19;
    public const int e6 = 20;
    public const int f6 = 21;
    public const int g6 = 22;
    public const int h6 = 23;

    public const int a3 = 40;
    public const int b3 = 41;
    public const int c3 = 42;
    public const int d3 = 43;
    public const int e3 = 44;
    public const int f3 = 45;
    public const int g3 = 46;
    public const int h3 = 47;

    public const int a2 = 48;
    public const int b2 = 49;
    public const int c2 = 50;
    public const int d2 = 51;
    public const int e2 = 52;
    public const int f2 = 53;
    public const int g2 = 54;
    public const int h2 = 55;

    public const int a1 = 56;
    public const int b1 = 57;
    public const int c1 = 58;
    public const int d1 = 59;
    public const int e1 = 60;
    public const int f1 = 61;
    public const int g1 = 62;
    public const int h1 = 63;

    #endregion

    private const ulong DarkSquaresBitboard = 0x55AA55AA55AA55AA;

    /// <summary>
    /// Chebyshev Distance represents how many moves it takes for a king to go from A to B <br/>
    /// ChebyshevDistance[(r1, f1), (r2, f2)] = max(|r1-r2|, |f1-f2|)<br/>
    /// Chebyshev Distance lies in [0, 7]
    /// https://www.chessprogramming.org/Distance
    /// </summary>
    private static int[,] ChebyshevDist { get; } = new int[64, 64];

    /// <summary>
    /// Manhattan Distance represents how many orthogonal moves it takes a king (or how many 1-step moves it takes a rook) to go from A to B<br/>
    /// ManhattanDistance[(r1, f1), (r2, f2)] = |r1-r2| + |f1-f2|<br/>
    /// Manhattan Distance lies in [0, 14]
    /// </summary>
    private static int[,] ManhattanDist { get; } = new int[64, 64];

    static BoardRepresentation()
    {
        PrecalculateDistances();
    }

    private static void PrecalculateDistances()
    {
        for (int i = 0; i < 64; i++)
        {
            for (int j = i; j < 64; j++)
            {
                (int rank1, int file1) = (GetRankBySquare(i), GetFileBySquare(i));
                (int rank2, int file2) = (GetRankBySquare(j), GetFileBySquare(j));

                ChebyshevDist[i, j] = ChebyshevDist[j, i] = Math.Max(Math.Abs(rank1 - rank2), Math.Abs(file1 - file2));
                ManhattanDist[i, j] = ManhattanDist[j, i] = Math.Abs(rank1 - rank2) + Math.Abs(file1 - file2);
            }
        }
    }

    public static int SquareNameToPos(string squareName)
        => (8 - (squareName[1] - '0')) * 8 + (squareName[0] - 'a');

    public static string PosToSquareName(int pos)
        => (char)(GetFileBySquare(pos) + 'a') + (8 - GetRankBySquare(pos)).ToString();

    public static char FileIndexToFileName(int fileIndex)
        => (char)(fileIndex + 'a');

    public static int GetFileBySquare(int square)
        => square % 8;

    public static int GetRankBySquare(int square)
        => square / 8;

    public static bool SquareExists(int rank, int file)
        => 0 <= rank && rank <= 7 && 0 <= file && file <= 7;

    public static bool IsSquareWhite(int square)
    {
        return ((DarkSquaresBitboard >> square) & 1) == 0;
    }
}