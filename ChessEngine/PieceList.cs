using System;
using System.Linq;

namespace ChessEngine.ChessEngine;

public class PieceList
{    
    public int Count { get; private set; }
    public int[] OccupiedSquares;

    private int[] _pieceArrayIndexBySquare;

    public PieceList(int maxPieceCount)
    {
        OccupiedSquares = new int[maxPieceCount];
        _pieceArrayIndexBySquare = new int[64];
        Count = 0;
    }
    public PieceList(PieceList pieceList)
    {
        OccupiedSquares = new int[pieceList.OccupiedSquares.Length];
        Array.Copy(pieceList.OccupiedSquares, OccupiedSquares, OccupiedSquares.Length);

        _pieceArrayIndexBySquare = new int[pieceList._pieceArrayIndexBySquare.Length];
        Array.Copy(pieceList._pieceArrayIndexBySquare, _pieceArrayIndexBySquare, _pieceArrayIndexBySquare.Length);

        Count = pieceList.Count;
    }

    public void AddPiece(int square)
    {
        OccupiedSquares[Count] = square;
        _pieceArrayIndexBySquare[square] = Count;
        Count++;
    }

    public void RemovePiece(int square)
    {
        int pieceArrayIndex = _pieceArrayIndexBySquare[square]; 
        OccupiedSquares[pieceArrayIndex] = OccupiedSquares[Count - 1]; // Move last element to a newly vacant place
        _pieceArrayIndexBySquare[OccupiedSquares[pieceArrayIndex]] = pieceArrayIndex;
        Count--;
    }

    public void MovePiece(int startSquare, int targetSquare)
    {
        int pieceArrayIndex = _pieceArrayIndexBySquare[startSquare];
        OccupiedSquares[pieceArrayIndex] = targetSquare;
        _pieceArrayIndexBySquare[targetSquare] = pieceArrayIndex;
    }

    public int this[int index] => OccupiedSquares[index];

    public static bool Compare(PieceList a, PieceList b)
    {
        if (a.Count != b.Count)
            return false;

        var bList = b.OccupiedSquares.ToList();
        for (int i = 0; i < a.Count; i++)
            if (!bList.Contains(a.OccupiedSquares[i]))
                return false;

        return true;
    }
}
