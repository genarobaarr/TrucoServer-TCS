using System.Collections.Generic;
using System.ServiceModel;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Contracts
{
    [ServiceContract(CallbackContract = typeof(ITrucoTournamentCallback))]
    public interface ITrucoTournamentService
    {
        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        List<TournamentDTO> GetAvailableTournaments();

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        bool SubscribeToTournament(int tournamentId, int userId);

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        List<BracketDTO> GetTournamentTree(int tournamentId);
    }
}