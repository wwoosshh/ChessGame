using System;
using System.Windows;
using ChessGame.Core.Enums;
using ChessGame.WPF.Views;

namespace ChessGame.WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ShowMenu();
        }

        private void ShowMenu()
        {
            var menuView = new MenuView();
            menuView.StartGameRequested += OnStartGameRequested;
            menuView.StartCustomGameRequested += OnStartCustomGameRequested; // 새로 추가
            MainContent.Content = menuView;
        }

        private void OnStartGameRequested(object? sender, StartGameEventArgs e)
        {
            var gameView = new GameView(e.GameMode, e.AiDifficulty);
            gameView.BackToMenuRequested += OnBackToMenuRequested;
            MainContent.Content = gameView;
        }

        // 새로 추가: 커스텀 게임 시작
        private void OnStartCustomGameRequested(object? sender, EventArgs e)
        {
            var customSetupView = new CustomBoardSetupView();
            customSetupView.CustomGameStartRequested += OnCustomGameStartRequested;
            customSetupView.BackToMenuRequested += OnBackToMenuRequested;
            MainContent.Content = customSetupView;
        }

        // 새로 추가: 커스텀 게임 실제 시작
        private void OnCustomGameStartRequested(object? sender, CustomGameStartEventArgs e)
        {
            var gameView = new GameView(GameMode.Custom, e.AiDifficulty, e.CustomBoard,
                                       e.FirstPlayer, e.AllowCastling, e.AllowEnPassant);
            gameView.BackToMenuRequested += OnBackToMenuRequested;
            MainContent.Content = gameView;
        }

        private void OnBackToMenuRequested(object? sender, EventArgs e)
        {
            ShowMenu();
        }
    }
}