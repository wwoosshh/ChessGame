using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChessGame.AI.Engines;
using ChessGame.Core.Enums;
using ChessGame.Core.Models.Game;

namespace ChessGame.WPF.Controls
{
    public partial class MoveEvaluationControl : UserControl
    {
        private ObservableCollection<MoveEvaluationItem> _moves;

        public MoveEvaluationControl()
        {
            InitializeComponent();
            _moves = new ObservableCollection<MoveEvaluationItem>();
            MovesList.ItemsSource = _moves;
        }

        public void AddMoveEvaluation(Move move, MoveQuality quality, int moveNumber, bool isWhite)
        {
            var item = new MoveEvaluationItem
            {
                MoveNumber = isWhite ? $"{moveNumber}." : $"{moveNumber}...",
                MoveText = FormatMoveText(move),
                Quality = GetQualitySymbol(quality),
                QualityColor = GetQualityColor(quality),
                BackgroundColor = GetBackgroundColor(quality)
            };

            _moves.Insert(0, item);

            while (_moves.Count > 20)
            {
                _moves.RemoveAt(_moves.Count - 1);
            }
        }
        private string FormatMoveText(Move move)
        {
            // 대수 표기법으로 변환 (예: e4, Nf3 등)
            if (move.MovedPiece == null)
                return move.ToNotation();

            string notation = "";

            // 기물 기호 (폰은 생략)
            if (move.MovedPiece.Type != PieceType.Pawn)
            {
                notation += move.MovedPiece.GetSymbol();
            }

            // 캡처인 경우
            if (move.IsCapture)
            {
                if (move.MovedPiece.Type == PieceType.Pawn)
                {
                    // 폰 캡처는 출발 파일 표시
                    notation += (char)('a' + move.From.Column);
                }
                notation += "x";
            }

            // 도착 위치
            notation += move.To.ToNotation();

            // 특수 표기
            if (move.IsPromotion)
                notation += "=" + (move.PromotionPiece?.ToString()[0] ?? 'Q');
            if (move.IsCheck)
                notation += "+";
            if (move.IsCheckmate)
                notation += "#";

            return notation;
        }
        private string GetQualitySymbol(MoveQuality quality)
        {
            return quality switch
            {
                MoveQuality.Brilliant => "!!",
                MoveQuality.Good => "!",
                MoveQuality.Best => "*",
                MoveQuality.Book => "",  // 이론적인 수는 기호 없음
                MoveQuality.Dubious => "?!",
                MoveQuality.Mistake => "?",
                MoveQuality.Blunder => "??",
                _ => ""
            };
        }

        private Brush GetQualityColor(MoveQuality quality)
        {
            return quality switch
            {
                MoveQuality.Brilliant => new SolidColorBrush(Color.FromRgb(218, 165, 32)),  // 금색
                MoveQuality.Good => new SolidColorBrush(Color.FromRgb(34, 139, 34)),       // 초록색
                MoveQuality.Best => new SolidColorBrush(Color.FromRgb(30, 144, 255)),      // 파란색
                MoveQuality.Book => new SolidColorBrush(Color.FromRgb(128, 128, 128)),     // 회색
                MoveQuality.Dubious => new SolidColorBrush(Color.FromRgb(255, 140, 0)),    // 주황색
                MoveQuality.Mistake => new SolidColorBrush(Color.FromRgb(220, 20, 60)),    // 빨간색
                MoveQuality.Blunder => new SolidColorBrush(Color.FromRgb(139, 0, 0)),      // 진한 빨간색
                _ => Brushes.Black
            };
        }

        private Brush GetBackgroundColor(MoveQuality quality)
        {
            return quality switch
            {
                MoveQuality.Brilliant => new SolidColorBrush(Color.FromArgb(40, 218, 165, 32)),
                MoveQuality.Good => new SolidColorBrush(Color.FromArgb(40, 34, 139, 34)),
                MoveQuality.Best => new SolidColorBrush(Color.FromArgb(40, 30, 144, 255)),
                MoveQuality.Book => new SolidColorBrush(Color.FromArgb(20, 128, 128, 128)),
                MoveQuality.Dubious => new SolidColorBrush(Color.FromArgb(40, 255, 140, 0)),
                MoveQuality.Mistake => new SolidColorBrush(Color.FromArgb(40, 220, 20, 60)),
                MoveQuality.Blunder => new SolidColorBrush(Color.FromArgb(40, 139, 0, 0)),
                _ => Brushes.Transparent
            };
        }

        public void Clear()
        {
            _moves.Clear();
        }
    }

    public class MoveEvaluationItem
    {
        public string MoveNumber { get; set; } = "";
        public string MoveText { get; set; } = "";
        public string Quality { get; set; } = "";
        public Brush QualityColor { get; set; } = Brushes.Black;
        public Brush BackgroundColor { get; set; } = Brushes.Transparent;
    }
}