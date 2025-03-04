using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace LukeBot.Tests.Main
{
    [TestClass]
    public class ServerCLITests
    {
        [ClassInitialize]
        public static void ServerCLI_TestClassStartup(TestContext context)
        {
        }

        [TestInitialize]
        public void ServerCLI_TestInitialize()
        {
        }

        [TestCleanup]
        public void ServerCLI_Cleanup()
        {
        }

        [ClassCleanup]
        public static void ServerCLI_TestClassTeardown()
        {
        }


        [TestMethod]
        [Ignore]
        public void ServerCLI_Connect_CorruptedFrame()
        {
            // TODO write this test
            // It should refer to this Exception being thrown:
            // 427594.9173 [ ERROR ] LukeBot\ServerCLI.cs @ 725 <MainLoop>: System.Security.Authentication.AuthenticationException: Cannot determine the frame size or a corrupted frame was received.
            //    at System.Net.Security.SslStream.EnsureFullTlsFrameAsync[TIOAdapter](CancellationToken cancellationToken, Int32 estimatedSize)
            //    at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)
            //    at System.Net.Security.SslStream.ReceiveHandshakeFrameAsync[TIOAdapter](CancellationToken cancellationToken)
            //    at System.Net.Security.SslStream.ForceAuthenticationAsync[TIOAdapter](Boolean receiveFirst, Byte[] reAuthenticationData, CancellationToken cancellationToken)
            //    at System.Net.Security.SslStream.AuthenticateAsServer(SslServerAuthenticationOptions sslServerAuthenticationOptions)
            //    at LukeBot.ServerCLI.AcceptNewConnection() in E:\DEV Projekty\GitHub\LukeBot\LukeBot\ServerCLI.cs:line 521
            //    at LukeBot.ServerCLI.MainLoop() in E:\DEV Projekty\GitHub\LukeBot\LukeBot\ServerCLI.cs:line 693 caught during ServerCLI operation: Cannot determine the frame size or a corrupted frame was received.
        }
    }
}
