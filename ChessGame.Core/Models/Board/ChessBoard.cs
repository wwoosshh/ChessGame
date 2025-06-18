using System.Net.NetworkInformation;
using ChessGame.Core.Enums;
using ChessGame.Core.Models.Pieces.Abstract;
using ChessGame.Core.Models.Pieces.Standard;
using ChessGame.Core.Models.Pieces.Fairy;

namespace ChessGame.Core.Models.Board
{
    public class ChessBoard
    {
        private Square[,] _squares;
        public const int Size = 8;
        public bool AllowCastling { get; set; } = true;
        public bool AllowEnPassant { get; set; } = true;

        public ChessBoard()
        {
            _squares = new Square[Size, Size];
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            // 모든 칸 초기화
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    _squares[row, col] = new Square(new Position(row, col));
                }
            }
        }

        public void SetupStandardPosition()
        {
            // 백색 기물 배치
            SetPiece(new Position(0, 0), new Rook(PieceColor.White));
            SetPiece(new Position(0, 1), new Knight(PieceColor.White));
            SetPiece(new Position(0, 2), new Bishop(PieceColor.White));
            SetPiece(new Position(0, 3), new Queen(PieceColor.White));
            SetPiece(new Position(0, 4), new King(PieceColor.White));
            SetPiece(new Position(0, 5), new Bishop(PieceColor.White));
            SetPiece(new Position(0, 6), new Knight(PieceColor.White));
            SetPiece(new Position(0, 7), new Rook(PieceColor.White));

            // 백색 폰
            for (int col = 0; col < Size; col++)
            {
                SetPiece(new Position(1, col), new Pawn(PieceColor.White));
            }

            // 흑색 기물 배치
            SetPiece(new Position(7, 0), new Rook(PieceColor.Black));
            SetPiece(new Position(7, 1), new Knight(PieceColor.Black));
            SetPiece(new Position(7, 2), new Bishop(PieceColor.Black));
            SetPiece(new Position(7, 3), new Queen(PieceColor.Black));
            SetPiece(new Position(7, 4), new King(PieceColor.Black));
            SetPiece(new Position(7, 5), new Bishop(PieceColor.Black));
            SetPiece(new Position(7, 6), new Knight(PieceColor.Black));
            SetPiece(new Position(7, 7), new Rook(PieceColor.Black));

            // 흑색 폰
            for (int col = 0; col < Size; col++)
            {
                SetPiece(new Position(6, col), new Pawn(PieceColor.Black));
            }
        }

        // 커스텀 보드 설정
        public void SetupCustomPosition(Piece?[,] customBoard, bool allowCastling = true, bool allowEnPassant = true)
        {
            AllowCastling = allowCastling;
            AllowEnPassant = allowEnPassant;

            // 보드 초기화
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    SetPiece(new Position(row, col), null);
                }
            }

            // 커스텀 배치 적용
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    var piece = customBoard[row, col];
                    if (piece != null)
                    {
                        var clonedPiece = ClonePiece(piece);
                        SetPiece(new Position(row, col), clonedPiece);
                    }
                }
            }
        }

        private Piece? ClonePiece(Piece piece)
        {
            Piece cloned;

            switch (piece.Type)
            {
                case PieceType.King:
                    cloned = new King(piece.Color);
                    break;
                case PieceType.Queen:
                    cloned = new Queen(piece.Color);
                    break;
                case PieceType.Rook:
                    cloned = new Rook(piece.Color);
                    break;
                case PieceType.Bishop:
                    cloned = new Bishop(piece.Color);
                    break;
                case PieceType.Knight:
                    cloned = new Knight(piece.Color);
                    break;
                case PieceType.Pawn:
                    cloned = new Pawn(piece.Color);
                    break;

                // 페어리 체스 기물들
                case PieceType.Archbishop:
                    cloned = new Archbishop(piece.Color);
                    break;
                case PieceType.Chancellor:
                    cloned = new Chancellor(piece.Color);
                    break;
                case PieceType.Amazon:
                    cloned = new Amazon(piece.Color);
                    break;
                case PieceType.Ferz:
                    cloned = new Ferz(piece.Color);
                    break;
                case PieceType.Wazir:
                    cloned = new Wazir(piece.Color);
                    break;
                case PieceType.Camel:
                    cloned = new Camel(piece.Color);
                    break;

                // 아직 구현되지 않은 기물들은 기본값으로 처리
                case PieceType.Zebra:
                case PieceType.Unicorn:
                case PieceType.Dragon:
                case PieceType.Gryphon:
                case PieceType.Nightrider:
                case PieceType.Grasshopper:
                case PieceType.Centaur:
                case PieceType.Mann:
                case PieceType.Guard:
                    // 임시로 Ferz로 처리 (나중에 구현)
                    cloned = new Ferz(piece.Color);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown piece type: {piece.Type}");
            }

            cloned.HasMoved = piece.HasMoved;
            return cloned;
        }

        public Square GetSquare(Position position)
        {
            if (!position.IsValid())
                throw new ArgumentException("Invalid position");

            return _squares[position.Row, position.Column];
        }

        public Piece? GetPiece(Position position)
        {
            return GetSquare(position).Piece;
        }

        public void SetPiece(Position position, Piece? piece)
        {
            GetSquare(position).Piece = piece;
        }

        public bool IsEmpty(Position position)
        {
            return GetSquare(position).IsEmpty;
        }

        public void MovePiece(Position from, Position to)
        {
            var piece = GetPiece(from);
            if (piece == null)
                throw new InvalidOperationException("No piece at source position");

            SetPiece(to, piece);
            SetPiece(from, null);
            piece.HasMoved = true;
        }

        public ChessBoard Clone()
        {
            var clone = new ChessBoard();
            clone.AllowCastling = this.AllowCastling;
            clone.AllowEnPassant = this.AllowEnPassant;

            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    var position = new Position(row, col);
                    var piece = GetPiece(position);
                    if (piece != null)
                    {
                        var clonedPiece = ClonePiece(piece);
                        clone.SetPiece(position, clonedPiece);
                    }
                }
            }
            return clone;
        }

        public List<Piece> GetPieces(PieceColor color)
        {
            var pieces = new List<Piece>();
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    var piece = _squares[row, col].Piece;
                    if (piece != null && piece.Color == color)
                        pieces.Add(piece);
                }
            }
            return pieces;
        }

        public Position? FindKing(PieceColor color)
        {
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    var piece = _squares[row, col].Piece;
                    if (piece != null && piece.Type == PieceType.King && piece.Color == color)
                        return new Position(row, col);
                }
            }
            return null;
        }

        // 확장: 특정 타입의 기물들 찾기
        public List<Position> FindPiecesByType(PieceType pieceType, PieceColor? color = null)
        {
            var positions = new List<Position>();

            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    var piece = _squares[row, col].Piece;
                    if (piece != null && piece.Type == pieceType)
                    {
                        if (color == null || piece.Color == color)
                        {
                            positions.Add(new Position(row, col));
                        }
                    }
                }
            }

            return positions;
        }

        // 확장: 보드 유효성 검사
        public bool ValidateBoard()
        {
            var whiteKings = FindPiecesByType(PieceType.King, PieceColor.White);
            var blackKings = FindPiecesByType(PieceType.King, PieceColor.Black);

            // 각 색깔마다 킹이 정확히 하나씩 있어야 함
            return whiteKings.Count == 1 && blackKings.Count == 1;
        }

        // 확장: 기물 통계
        public Dictionary<PieceType, int> GetPieceCount(PieceColor color)
        {
            var count = new Dictionary<PieceType, int>();

            foreach (PieceType pieceType in Enum.GetValues<PieceType>())
            {
                count[pieceType] = 0;
            }

            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    var piece = _squares[row, col].Piece;
                    if (piece != null && piece.Color == color)
                    {
                        count[piece.Type]++;
                    }
                }
            }

            return count;
        }

        // 확장: 기물 가치 총합 계산 (페어리 기물 포함)
        public int CalculateMaterialValue(PieceColor color)
        {
            int totalValue = 0;

            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    var piece = _squares[row, col].Piece;
                    if (piece != null && piece.Color == color)
                    {
                        totalValue += piece.PointValue;
                    }
                }
            }

            return totalValue;
        }
    }
}