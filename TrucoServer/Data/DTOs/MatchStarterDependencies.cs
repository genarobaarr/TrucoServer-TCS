using TrucoServer.GameLogic;
using TrucoServer.Helpers.Match;

namespace TrucoServer.Data.DTOs
{
    public class MatchStarterDependencies
    {
        public baseDatosTrucoEntities Context { get; set; }
        public IGameRegistry GameRegistry { get; set; }
        public ILobbyCoordinator Coordinator { get; set; }
        public ILobbyRepository Repository { get; set; }
        public IDeckShuffler Shuffler { get; set; }
        public IGameManager GameManager { get; set; }
        public GamePlayerBuilder ParticipantBuilder { get; set; }
        public ListPositionService PositionService { get; set; }
    }
}
