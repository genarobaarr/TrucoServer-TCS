using System.Collections.Generic;
using System.ServiceModel;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Contracts
{
    [ServiceContract]
    public interface ITrucoTournamentCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnTournamentPlayerJoined(string username, int currentCapacity);

        [OperationContract(IsOneWay = true)]
        void OnTournamentPlayerLeft(string username, int currentCapacity);

        [OperationContract(IsOneWay = true)]
        void OnTournamentStarted(List<BracketDTO> initialBrackets);

        [OperationContract(IsOneWay = true)]
        void OnTournamentCancelled(string reason);

        [OperationContract(IsOneWay = true)]
        void OnBracketUpdated(BracketDTO updatedBracket);

        [OperationContract(IsOneWay = true)]
        void OnTournamentEliminated();

        [OperationContract(IsOneWay = true)]
        void OnAdvanceToFinal(string matchCode);
    }
}