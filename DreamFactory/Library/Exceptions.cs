using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;

namespace OpenNETCF.DreamFactory
{
    public class DreamFactoryException : Exception
    {
        public ErrorDescriptor ServerError { get; private set; }

        public DreamFactoryException(string message)
            : base(message)
        {
        }

        public DreamFactoryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal DreamFactoryException(ErrorDescriptor descriptor, string message)
            : base(message)
        {
            ServerError = descriptor;
        }

        internal DreamFactoryException(ErrorDescriptor descriptor)
            : base(descriptor.message)
        {
            ServerError = descriptor;
        }

        internal static DreamFactoryException Parse(IRestResponse response)
        {
            ErrorDescriptorList errorList;

            try
            {
                // attempt to parse it as error JSON
                errorList = SimpleJson.DeserializeObject<ErrorDescriptorList>(response.Content);
            }
            catch
            {
                return new DreamFactoryException(string.Format("Server responded with an error {0} ({1})", (int)response.StatusCode, response.StatusCode));
            }

            if (Debugger.IsAttached) Debugger.Break();

            if (errorList.error.Count > 0)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        if (errorList.error[0].message == "There is no valid session for the current request.")
                        {
                            return new InvalidSessionException(new DreamFactoryException(errorList.error[0]));
                        }
                        else
                        {
                            return new DreamFactoryException(errorList.error[0]);
                        }
                    default:
                        return new DreamFactoryException(errorList.error[0]);
                }
            }
            return new DreamFactoryException(response.StatusDescription);
        }

        internal static Exception ValidateIRestResponse(IRestResponse response)
        {
            switch (response.ResponseStatus)
            {
                case ResponseStatus.Error:
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.GatewayTimeout:
                            return new DreamFactoryException("Error 504: Gateway Timeout", response.ErrorException);
                        default:
                            return new DreamFactoryException("Response Error: " + response.ErrorMessage, response.ErrorException);
                    }
                case ResponseStatus.TimedOut:
                    return new DreamFactoryException("Timeout", response.ErrorException);
                default:
                    return null;
            }
        }
    }

    public class InvalidSessionException : DreamFactoryException
    {
        public InvalidSessionException(Exception innerException)
            : base("The current Session is invalid", innerException)
        {
        }
    }

    public class DeserializationException : DreamFactoryException
    {
        public DeserializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class TableNotFoundException : DreamFactoryException
    {
        public TableNotFoundException(string tableName)
            : base(string.Format("Table not found: '{0}'", tableName))
        {
        }
    }

    public class InvalidCredentialsException : DreamFactoryException
    {
        public InvalidCredentialsException(ErrorDescriptor descriptor)
            : base(descriptor, "Invalid username or password")
        {
        }
    }

    public class UserNotAuthorizedException : DreamFactoryException
    {
        public UserNotAuthorizedException(ErrorDescriptor descriptor)
            : base(descriptor, "The current user is not authorized to perform the selected action")
        {
        }
    }
}
