namespace ChessEngine.ChessEngine;

public readonly struct Move
{
    private const int InvalidMoveValue = 0;
    
    /// <summary>
    /// Flags are defined in such a way that these info can be get without multiple conditions:<br/>
    /// 1) A move is promotion if the 3rd bit is '1' (1xxx - promotion, 0xxx - not promotion)<br/>
    /// 2) For promotion moves, bits 0-2 form a bitstring of a piece that a pawn was promoted to (for example, since Piece.QUEEN is 0b101, promotion to a queen is 0b1_101)
    /// </summary>
    public readonly struct MoveFlag
    {
        public const int None = 0b000;
        public const int Castle = 0b001; 
        public const int EnPassantCapture = 0b100;
        public const int PawnDoubleMove = 0b101;
        public const int PromotionToQueen = 0b1101; 
        public const int PromotionToRook = 0b1100; 
        public const int PromotionToKnight = 0b1010; 
        public const int PromotionToBishop = 0b1011; 
    }

    /// <summary>
    /// Move is represented as a 16-bit number:
    /// Bits 0-5 represent start square
    /// Bits 6-11 represent target square
    /// Bits 12-15 represent flag
    /// </summary>
    private readonly ushort _value;
    public ushort Value => _value;
    
    public int StartSquare => _value & 0b111111;
    public int TargetSquare => (_value >> 6) & 0b111111;
    public int Flag => (_value >> 12) & 0b1111;
    public bool IsPromotion => ((_value >> 15) & 0b1) == 1;
    public int PromotedPieceType => (_value >> 12) & 0b111;
    public bool IsInvalid => _value == InvalidMoveValue;
    public bool IsValid => _value != InvalidMoveValue;

    public static readonly Move InvalidMove = new Move(InvalidMoveValue);
    
    #region Contructors

    public Move(ushort value)
    {
        _value = value;
    }

    /// <summary>
    /// Sets flag to MoveFlag.NONE (=0)
    /// </summary>
    public Move(int startSquare, int targetSquare)
    {
        _value = (ushort) (startSquare | targetSquare << 6);
    }
    public Move(int startSquare, int targetSquare, int flag)
    {
        _value = (ushort) (startSquare | targetSquare << 6 | flag << 12);
    }

    #endregion
    
    public override bool Equals(object? obj)
    {
        return (obj is Move move && _value == move._value);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public override string ToString()
    {
        return $"{BoardRepresentation.PosToSquareName(StartSquare)}-{BoardRepresentation.PosToSquareName(TargetSquare)}";
    }
}
