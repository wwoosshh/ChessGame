// ChessGame.Core/Enums/PieceType.cs
namespace ChessGame.Core.Enums
{
    public enum PieceType
    {
        // 표준 기물
        King,
        Queen,
        Rook,
        Bishop,
        Knight,
        Pawn,

        // 페어리 체스 기물들
        Archbishop,  // 비숍 + 나이트 (대각선 + L자)
        Chancellor,  // 룩 + 나이트 (직선 + L자)
        Amazon,      // 퀸 + 나이트 (모든 방향 + L자)
        Ferz,        // 대각선 1칸만
        Wazir,       // 직선 1칸만 (상하좌우)
        Camel,       // (3,1) 점프
        Zebra,       // (3,2) 점프
        Unicorn,     // 나이트 + 비숍
        Dragon,      // 룩 + 킹 (직선 무한 + 대각선 1칸)
        Gryphon,     // 비숍 + 킹 (대각선 무한 + 직선 1칸)
        Nightrider,  // 나이트 방향으로 계속 이동
        Grasshopper, // 직선으로 이동하되 다른 기물을 뛰어넘어야 함
        Centaur,     // 킹 + 나이트 (인접 8칸 + L자)
        Mann,        // 킹처럼 이동하지만 킹이 아님
        Guard        // 킹 1칸 이동만 (체크 무시)
    }
}