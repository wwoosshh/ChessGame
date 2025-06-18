using System;
using System.Windows;
using System.Windows.Controls;
using ChessGame.Core.Enums;

namespace ChessGame.WPF.Views
{
    public partial class MenuView : UserControl
    {
        public event EventHandler<StartGameEventArgs>? StartGameRequested;
        public event EventHandler? StartCustomGameRequested; // 새로 추가

        public MenuView()
        {
            InitializeComponent();
        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            var gameMode = StandardModeRadio.IsChecked == true ? GameMode.Standard : GameMode.Custom;

            var difficulty = AiDifficulty.Medium;
            if (EasyRadio.IsChecked == true)
                difficulty = AiDifficulty.Easy;
            else if (HardRadio.IsChecked == true)
                difficulty = AiDifficulty.Hard;

            StartGameRequested?.Invoke(this, new StartGameEventArgs
            {
                GameMode = gameMode,
                AiDifficulty = difficulty
            });
        }

        // 새로 추가: 커스텀 모드 설정 버튼
        private void CustomSetupButton_Click(object sender, RoutedEventArgs e)
        {
            StartCustomGameRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }

    public class StartGameEventArgs : EventArgs
    {
        public GameMode GameMode { get; set; }
        public AiDifficulty AiDifficulty { get; set; }
    }
}