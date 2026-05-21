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
        string CreateTournament(int capacity, int hostUserId);

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        bool JoinTournamentByCode(string code, int userId);

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        bool StartTournament(string code, int hostUserId);

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        bool LeaveTournament(string code, int userId);

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        List<BracketDTO> GetTournamentTree(int tournamentId);

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        void ReportMatchResult(string tournamentCode, string matchCode, int winnerUserId);

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        List<TournamentDTO> GetAvailableTournaments();

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        void UpdateBracketMatchCode(string tournamentCode, string oldMatchCode, string newMatchCode);

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        List<string> GetTournamentParticipants(string code);
    }
}