using System;
using System.IO;
using System.Text.Json;

namespace ChessEngine.ChessEngine;

public static class ZobristHashing
{
    public static readonly ulong[,,] PiecesOnSquaresHashes = new ulong[2, 7, 64];
    public static readonly ulong BlackToMoveHash;
    public static readonly ulong[] EnPassantFileHashes = new ulong[8];
    public static readonly ulong[] CastlingRightsHashes = new ulong[16]; // castling rights are defined by 4 bits

    static ZobristHashing()
    {
        ulong[] randomNums;
        string path = "/hashes.txt";

        if (!File.Exists(path))
        {
            GenerateRandomNumbers();
        }

        using (StreamReader reader = new StreamReader(path))
        {
            randomNums = JsonSerializer.Deserialize<ulong[]>(reader.ReadToEnd()) ??
                         throw new InvalidOperationException();
        }

        int index = 0;
        for (int piece = 0; piece < 7; piece++)
        {
            for (int place = 0; place < 64; place++)
            {
                PiecesOnSquaresHashes[Board.WhiteIndex, piece, place] = randomNums[index++];
                PiecesOnSquaresHashes[Board.BlackIndex, piece, place] = randomNums[index++];
            }
        }

        BlackToMoveHash = randomNums[index++];

        for (int i = 0; i < 8; i++)
            EnPassantFileHashes[i] = randomNums[index++];

        for (int i = 0; i < 16; i++)
            CastlingRightsHashes[i] = randomNums[index++];
    }

    public static ulong GetZobristHash(Board board)
    {
        ulong hash = 0;

        for (int pos = 0; pos < 64; pos++)
        {
            int piece = board.GetPiece(pos);
            if (piece != PieceManager.None)
            {
                hash ^= PiecesOnSquaresHashes[PieceManager.IsWhite(piece) ? Board.WhiteIndex : Board.BlackIndex,
                    PieceManager.GetType(piece),
                    pos];
            }
        }

        if (board.IsWhiteMove() == false)
            hash ^= BlackToMoveHash;

        int epFile = board.GetEnPassantFile();
        if (epFile != Board.NoEnPassant)
            hash ^= EnPassantFileHashes[epFile];

        hash ^= CastlingRightsHashes[board.GetCastlingRights()];

        return hash;
    }

    private static ulong GetRandomUnsigned64BitNumber(in Random rnd)
    {
        var buffer = new byte[sizeof(ulong)];
        rnd.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }

    private static void GenerateRandomNumbers()
    {
        Random rnd = new Random();
        int count = 2 * 7 * 64 + 1 + 8 + 16;
        string path = "/hashes.txt";

        ulong[] ar = new ulong[count];
        for (int i = 0; i < count; i++)
            ar[i] = GetRandomUnsigned64BitNumber(rnd);

        using StreamWriter writer = new StreamWriter(path);
        writer.Write(JsonSerializer.Serialize(ar));
    }
}