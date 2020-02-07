using FluentFTP;
using Microsoft.Azure.Storage;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.NoOp;
using Polly.Registry;
using Polly.Timeout;
using System;

namespace Comvita.Common.Actor.FtpClient
{
    public interface IFtpPolicyRegistry
    {
        PolicyRegistry CreateRegistry();

        Policy GetPolicy(string key);
    }

    public class FtpNotConnectException : Exception
    {
        public FtpNotConnectException()
        {
        }

        public FtpNotConnectException(string message)
            : base(message)
        {
        }

        public FtpNotConnectException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public static class FtpPolicyName
    {
        public const string FtpSimpleRetryPolicy = "SimpleRetry";
        public const string FtpConnectPolicy = "ftpConnectPolicy";
        public const string FtpCommandPolicy = "ftpCommandPolicy";
        public const string FtpRetryAndWaitPolicy = "ftpRetryAndWaitPolicy";

        //Should find a better way to deal with policy
        public const string StorageRetryAndWaitPolicy = "storageCommandPolicy";
    }

    public class FtpPolicyRegistry : IFtpPolicyRegistry
    {
        private readonly ILogger _logger;

        public FtpPolicyRegistry(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FtpPolicyRegistry>();
        }

        #region Members

        public static PolicyRegistry Registry;

        #endregion Members

        public PolicyRegistry CreateRegistry()
        {
            Registry = new PolicyRegistry();
            var testPolicy = Policy.Handle<Exception>().WaitAndRetry(new[] {
                 TimeSpan.FromSeconds(1),
                 TimeSpan.FromSeconds(2),
                 TimeSpan.FromSeconds(30),
                 TimeSpan.FromSeconds(60),
            }).WithPolicyKey(FtpPolicyName.FtpSimpleRetryPolicy);

            //Security Not negotiated successfully
            var negotiatedPolicy = Policy.Handle<FtpSecurityNotAvailableException>().Fallback(fallbackAction: (exception, context, token) =>
            {
                throw exception;
            },
            onFallback: (e, context) =>
            {
                _logger.LogError(e, $"Failed FTP Security Not Available host at UTC: {DateTime.UtcNow.ToString()} from source: {e.Source}. Message: {e.Message}. HResult code: {e.HResult} .Falling back...");
            });

            var retryAndWait = Policy.Handle<FtpException>().WaitAndRetry(new[] {
                 TimeSpan.FromSeconds(1),
                 TimeSpan.FromSeconds(2),
                 TimeSpan.FromSeconds(5),
                 TimeSpan.FromSeconds(10)
            }).WithPolicyKey(FtpPolicyName.FtpRetryAndWaitPolicy);

            // use this for fail connect
            var connectPolicy = Policy.Wrap(
                Policy.Handle<Exception>().Fallback(
                    fallbackAction: /* Demonstrates fallback action/func syntax */ (exception, context, token) =>
                    {
                        throw new FtpNotConnectException(exception.Message);
                        //TODO: this is severe, need to notify
                    },
                    onFallback: (e, context) =>
                    {
                        _logger.LogError(e, $"Failed to connect to FTP host at UTC: {DateTime.UtcNow.ToString()} from source: {e.Source}. Message: {e.Message}. HResult code: {e.HResult} .Falling back...");
                    }
                )
                , Policy.Handle<Exception>(e => !(e is BrokenCircuitException)).Or<TimeoutRejectedException>().WaitAndRetry(new[] {
                 TimeSpan.FromSeconds(1),
                 TimeSpan.FromSeconds(2),
                 TimeSpan.FromSeconds(5),
                 TimeSpan.FromSeconds(10),
                 TimeSpan.FromSeconds(60)
                }, (exception, retryCount) =>
                 {
                     _logger.LogError(exception, $"Polly Connecting to FTP client failed due to {exception.Message}. Retry connecting...");
                 })
               , Policy.Timeout(60)).WithPolicyKey(FtpPolicyName.FtpConnectPolicy);

            //use this with fail command
            var ftpTransientCommandPolicy = Policy.Handle<FtpCommandException>(ex => ex.ResponseType == FtpResponseType.TransientNegativeCompletion).Or<FtpException>()
                .WaitAndRetry(new[] {
                 TimeSpan.FromSeconds(5),
                 TimeSpan.FromSeconds(10),
                 TimeSpan.FromSeconds(15)
            }, (exception, retryTimeSpan, context) =>
                 {
                     //log each time try
                     _logger.LogError(exception, $"Polly FTP Command {context.OperationKey}  failed to executed. {exception.Message}. Transient retry connecting...");
                 });

            var ftpPermanentCommandPolicy = Policy.Handle<FtpCommandException>(ex => ex.ResponseType == FtpResponseType.PermanentNegativeCompletion).WaitAndRetry(new[] {
                 TimeSpan.FromSeconds(1),
                 TimeSpan.FromSeconds(2) }, (exception, retryTimeSpan, context) =>
                 {
                     //log each time try
                     _logger.LogError(exception, $"Polly FTP Command {context.OperationKey}  failed to executed. {exception.Message}. PermanentNegative retry connecting...");
                 });

            var ftpCommandPolicy = Policy.Wrap(ftpPermanentCommandPolicy, ftpTransientCommandPolicy).WithPolicyKey(FtpPolicyName.FtpCommandPolicy);

            var azureStorageCommand = Policy.Handle<StorageException>().Or<TimeoutRejectedException>().WaitAndRetry(new[] {TimeSpan.FromSeconds(1),
                 TimeSpan.FromSeconds(2),
                 TimeSpan.FromSeconds(5)
            }, (exception, retryCount) =>
                 {
                     _logger.LogError(exception, $"Azure storage action failed due to {exception.Message}. Retry the action...");
                     //log each time try
                 }).Wrap(Policy.Timeout(60)).WithPolicyKey(FtpPolicyName.StorageRetryAndWaitPolicy);

            //azure storage command
            Registry.Add(FtpPolicyName.FtpSimpleRetryPolicy, testPolicy);
            Registry.Add(FtpPolicyName.FtpConnectPolicy, connectPolicy);
            Registry.Add(FtpPolicyName.FtpCommandPolicy, ftpCommandPolicy);
            Registry.Add(FtpPolicyName.FtpRetryAndWaitPolicy, retryAndWait);
            Registry.Add(FtpPolicyName.StorageRetryAndWaitPolicy, azureStorageCommand);

            return Registry;
        }

        public Policy GetPolicy(string key)
        {
            var policyExists = Registry.ContainsKey(key);
            if (!policyExists)
            {
                // if policy not exist return noop policy for passing through policy
                NoOpPolicy noOp = Policy.NoOp();
                return noOp;
            }
            return Registry.Get<Policy>(key);
        }
    }
}