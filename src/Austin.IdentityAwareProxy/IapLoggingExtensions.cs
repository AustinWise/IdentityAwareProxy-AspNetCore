using System.Net;
using Microsoft.Extensions.Logging;

namespace Austin.IdentityAwareProxy
{
    internal static partial class IapLoggingExtensions
    {
        [LoggerMessage(1, LogLevel.Critical, "Connection from unexpected IP address: {address}", EventName = "UnexpectedIpAddress")]
        public static partial void UnexpectedIpAddress(this ILogger logger, IPAddress address);

        [LoggerMessage(2, LogLevel.Critical, "The request was missing the IAP header: {header}", EventName = "MissingHeader")]
        public static partial void MissingHeader(this ILogger logger, string header);

        [LoggerMessage(3, LogLevel.Critical, "The IAP JWT was invalid: {jwtStr}", EventName = "InvalidJwt")]
        public static partial void InvalidJwt(this ILogger logger, string? jwtStr, Exception exception);

        [LoggerMessage(4, LogLevel.Critical, "The IAP JWT was invalid, but unexpectedly contains an unauthenticated user. If you want to allow unauthenticated users, set the AllowPublicAccess setting to true.", EventName = "UnexpectedUnauthenticatedUser")]
        public static partial void UnexpectedUnauthenticatedUser(this ILogger logger);

        [LoggerMessage(5, LogLevel.Error, "An exception was thrown while creating the principal.", EventName = "FailureCreatingPrincipal")]
        public static partial void FailureCreatingPrincipal(this ILogger logger, Exception exception);

        [LoggerMessage(6, LogLevel.Debug, "Successfully created IAP principal.", EventName = "SuccessfullyCreatePrincipal")]
        public static partial void SuccessfullyCreatedPrincipal(this ILogger logger);

        [LoggerMessage(7, LogLevel.Critical, "The IAP Simulator is configured and recieved an incoming request from somewhere other than localhost. Do not use the IAP Simulator in production.")]
        public static partial void SimulatorExposed(this ILogger logger);
    }
}
