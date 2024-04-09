// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Common.Extension
{
    public static class ExceptionExtension
    {
        public static bool IsRetryableHttpException(this HttpRequestException exception)
        {
            if (
                exception.InnerException is SocketException socketException
                && socketException.IsRetryableSocketException()
            )
            {
                return true;
            }
            if (exception.InnerException is IOException iOException && iOException.IsRetryableIOException())
            {
                return true;
            }
            return exception.InnerException != null
                && exception.InnerException.GetType().Name == "WinHttpException"
                && exception.InnerException.Message
                    == "A connection with the server could not be established" // 12029
            ;
        }

        public static bool IsRetryableIOException(this IOException iOException)
        {
            return iOException.Message.StartsWith("The response ended prematurely.");
        }

        public static bool IsRetryableSocketException(this SocketException socketException)
        {
            // SocketError.ConnectionReset
            //   "No connection could be made because the target machine actively refused it" 10054 (Windows)
            //   "Connection reset by peer" 10054 (Linux)
            // SocketError.ConnectionRefused
            //   "An existing connection was forcibly closed by the remote host") 10061 (Windows)
            //   "Connection refused"); // 10061 (Linux)
            return socketException.SocketErrorCode == SocketError.ConnectionReset
                || socketException.SocketErrorCode == SocketError.ConnectionRefused;
        }

        public static bool IsRetryableAggregateException(this AggregateException exception)
        {
            foreach (var innerException in exception.InnerExceptions)
            {
                if (innerException is SocketException socketException && socketException.IsRetryableSocketException())
                {
                    return true;
                }
                if (innerException is IOException iOException && iOException.IsRetryableIOException())
                {
                    return true;
                }
                if (
                    innerException.GetType().Name == "WinHttpException"
                    && innerException.Message == "A connection with the server could not be established" // 12029
                )
                {
                    return true;
                }
            }
            return false;
        }
    }
}
