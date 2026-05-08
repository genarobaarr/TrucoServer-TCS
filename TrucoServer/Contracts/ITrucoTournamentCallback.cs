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
        void OnTournamentStarted(List<BracketDTO> initialBrackets);

        [OperationContract(IsOneWay = true)]
        void OnBracketUpdated(BracketDTO updatedBracket);
    }
}