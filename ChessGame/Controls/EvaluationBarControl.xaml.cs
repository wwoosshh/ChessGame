using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ChessGame.AI.Engines;

namespace ChessGame.WPF.Controls
{
    public partial class EvaluationBarControl : UserControl
    {
        private const double MAX_EVAL = 10.0; // ±10 폰 이상은 압도적

        public EvaluationBarControl()
        {
            InitializeComponent();
        }

        public void UpdateEvaluation(EvaluationInfo evalInfo)
        {
            if (evalInfo.MateIn.HasValue)
            {
                ShowMateIndicator(evalInfo.MateIn.Value);
            }
            else
            {
                HideMateIndicator();
                UpdateBarPosition(evalInfo.CentipawnScore);
                UpdateScoreTexts(evalInfo.CentipawnScore);
            }
        }

        private void UpdateBarPosition(int centipawns)
        {
            // 흑색 관점에서는 반전
            double pawns = centipawns / 100.0;
            double normalizedScore = Math.Max(-MAX_EVAL, Math.Min(MAX_EVAL, pawns));

            // -10 ~ +10을 0 ~ 1로 정규화
            double ratio = (normalizedScore + MAX_EVAL) / (2 * MAX_EVAL);

            // 바의 높이 계산
            double totalHeight = ActualHeight - 100;
            double whiteHeight = totalHeight * ratio;

            // 애니메이션으로 부드럽게 이동
            var animation = new DoubleAnimation
            {
                To = whiteHeight,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            WhiteBar.BeginAnimation(HeightProperty, animation);
        }

        private void UpdateScoreTexts(int centipawns)
        {
            double pawns = Math.Abs(centipawns) / 100.0;
            string scoreText = pawns.ToString("F1");

            if (centipawns > 0)
            {
                WhiteScoreText.Text = scoreText;
                BlackScoreText.Text = "0.0";
            }
            else
            {
                WhiteScoreText.Text = "0.0";
                BlackScoreText.Text = scoreText;
            }
        }

        private void ShowMateIndicator(int mateIn)
        {
            MateIndicator.Text = $"M{Math.Abs(mateIn)}";
            MateIndicator.Visibility = Visibility.Visible;

            // 메이트 상황에서는 바를 극단으로
            if (mateIn > 0)
            {
                WhiteBar.Height = ActualHeight - 100;
                WhiteScoreText.Text = "M" + mateIn;
                BlackScoreText.Text = "0.0";
            }
            else
            {
                WhiteBar.Height = 0;
                WhiteScoreText.Text = "0.0";
                BlackScoreText.Text = "M" + Math.Abs(mateIn);
            }
        }

        private void HideMateIndicator()
        {
            MateIndicator.Visibility = Visibility.Collapsed;
        }
    }
}