using System.ServiceModel;
using TrucoServer.Data.DTOs;

namespace TrucoServer.Utilities
{
    public static class FaultFactory
    {
        public static FaultException<CustomFault> CreateFault(string errorCode, string errorMessage)
        {
            var fault = new CustomFault
            {
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
          
            return new FaultException<CustomFault>(fault, new FaultReason(errorCode));
        }
    }
}
