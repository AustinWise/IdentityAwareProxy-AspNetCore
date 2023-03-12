using Microsoft.Extensions.Logging;

namespace Austin.IdentityAwareProxy
{
    internal static partial class IapLoggingExtensions
    {
        [LoggerMessage(1, LogLevel.Critical, "The request was missing the IAP header.", EventName = "MissingHeader")]
        public static partial void MissingHeader(this ILogger logger);

        [LoggerMessage(2, LogLevel.Critical, "The IAP JWT was invalid.", EventName = "InvalidJwt")]
        public static partial void InvalidJwt(this ILogger logger, Exception exception);

        [LoggerMessage(3, LogLevel.Critical, "The IAP JWT was invalid, but unexpectedly contains an unauthenticated user. If you want to allow unauthenticated users, set the AllowPublicAccess setting to true.", EventName = "UnexpectedUnauthenticatedUser")]
        public static partial void UnexpectedUnauthenticatedUser(this ILogger logger);

        [LoggerMessage(4, LogLevel.Error, "An exception was thrown while creating the principal.", EventName = "FailureCreatingPrincipal")]
        public static partial void FailureCreatingPrincipal(this ILogger logger, Exception exception);

        [LoggerMessage(5, LogLevel.Debug, "Successfully created IAP principal.", EventName = "SuccessfullyCreatePrincipal")]
        public static partial void SuccessfullyCreatedPrincipal(this ILogger logger);
    }
}
