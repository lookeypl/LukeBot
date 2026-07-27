using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LukeBot.Config;
using LukeBot.Logging;


namespace LukeBot.Common
{
    public class Utils
    {
        #if OS_WINDOWS
        // WinAPI "reconstruction" to allow cancelling STDIN
        private const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CancelIoEx(IntPtr handle, IntPtr lpOverlapped);
        #elif OS_LINUX
        // TODO
        #else
        #error Platform not supported
        #endif // (WINDOWS)

        public static IntPtr GetHandleForStdin()
        {
            #if OS_WINDOWS
            return GetStdHandle(STD_INPUT_HANDLE);
            #else
            return 0;
            #endif
        }

        public static void CancelIo(IntPtr handle)
        {
            #if OS_WINDOWS
            CancelIoEx(handle, IntPtr.Zero);
            #endif
        }

        public static void CancelConsoleIO()
        {
            CancelIo(GetHandleForStdin());
        }

        public static string HttpStatusCodeToHTTPString(HttpStatusCode code)
        {
            // TODO not all codes are filled in cause I'm lazy. Maybe ArgumentException is thrown
            // because I was code is not on the list below. Fill it in some day.
            switch (code)
            {
            // 100s
            case HttpStatusCode.Continue: return "100 Continue";
            case HttpStatusCode.SwitchingProtocols: return "101 Switching Protocols";
            case HttpStatusCode.Processing: return "102 Processing";
            case HttpStatusCode.EarlyHints: return "103 Early Hints";
            // 200s
            case HttpStatusCode.OK: return "200 OK";
            case HttpStatusCode.Created: return "201 Created";
            case HttpStatusCode.Accepted: return "202 Accepted";
            case HttpStatusCode.NonAuthoritativeInformation: return "203 Non-Authoritative Information";
            case HttpStatusCode.NoContent: return "204 No Content";
            // 300s
            // 400s
            case HttpStatusCode.BadRequest: return "400 Bad Request";
            case HttpStatusCode.Unauthorized: return "401 Unauthorized";
            case HttpStatusCode.PaymentRequired: return "402 Payment Required";
            case HttpStatusCode.Forbidden: return "403 Forbidden";
            case HttpStatusCode.NotFound: return "404 Not Found";
            case HttpStatusCode.RequestTimeout: return "408 Request Timeout";
            case HttpStatusCode.Gone: return "410 Gone";
            // 500s
            case HttpStatusCode.InternalServerError: return "500 Internal Server Error";
            case HttpStatusCode.NotImplemented: return "501 Not Implemented";
            case HttpStatusCode.BadGateway: return "502 Bad Gateway";
            case HttpStatusCode.ServiceUnavailable: return "503 Service Unavailable";
            case HttpStatusCode.HttpVersionNotSupported: return "505 HTTP Version Not Supported";
            default:
                throw new ArgumentException(string.Format("Unsupported HTTP status code: {0}", code));
            }
        }

        public static Process StartBrowser(string url)
        {
            Process result = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                result = Process.Start("xdg-open", url);
            }
            else
            {
                throw new UnsupportedPlatformException("Platform is not supported");
            }

            return result;
        }

        // Parse a list of strings into a list of key-value tuples. Useful for providing arguments
        // to inner systems of LukeBot (ex. EventSystem's test command, or Widget's config update)
        // Notable parsing details:
        //  - Key always has to be a string without spaces
        //  - There must be no spaces surrounding the = sign, so always <key>=<value>
        //  - Longer strings with spaces are allowed if put in quotation marks
        //  - No escape characters are supported (yet) (TODO?)
        // Following args list is valid:
        //  Tier=2 Message="This is a message!" User=username
        // Produces three tuples (all strings):
        //  ("Tier", "2")
        //  ("Message", "This is a message!")
        //  ("User", "username")
        // TestEvent() will further parse the data for correctness against Event's
        // TestArgs list, if available.
        public static IEnumerable<(string attrib, string value)> ConvertArgStringsToTuples(IEnumerable<string> argsList)
        {
            List<(string attrib, string value)> ret = new();

            string a = "", v = "";
            bool readingString = false;
            foreach (string s in argsList)
            {
                if (readingString)
                {
                    if (s.EndsWith('"'))
                    {
                        readingString = false;
                        v += ' ' + s.Substring(0, s.Length - 1);
                        ret.Add((a, v));
                    }
                    else
                    {
                        v += ' ' + s;
                    }

                    continue;
                }

                string[] tokens = s.Split('=');
                if (tokens.Length != 2)
                {
                    throw new ArgumentException("Failed to parse test event attributes");
                }

                a = tokens[0];

                if (tokens[1].StartsWith('"'))
                {
                    v = tokens[1].Substring(1);
                    readingString = true;
                }
                else
                {
                    v = tokens[1];
                    ret.Add((a, v));
                }
            }

            return ret;
        }

        /**
         * Reads an input line masking what has been pressed. Useful for fetching sensitive
         * information, ex. passwords.
         *
         * @p showAsterisks allows to display asterisks instead of typed letters.
         */
        public static string ReadLineMasked(bool showAsterisks)
        {
            ConsoleKeyInfo key;
            string line = "";

            do
            {
                key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Backspace && line.Length > 0)
                {
                    line = line.Substring(0, line.Length - 1);
                    if (showAsterisks)
                        Console.Write("\b \b");
                }
                else if (key.Key != ConsoleKey.Enter)
                {
                    if (showAsterisks)
                        Console.Write('*');
                    line += key.KeyChar;
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.Write('\n');
            return line;
        }

        /**
         * Splits JSONs from a single string into a list of strings.
         *
         * This is useful for handling ex. all communication routines where multiple messages
         * might be sent in quick succession and received "at once" into single string.
         */
        public static List<string> SplitJSONs(string message)
        {
            List<string> messages = new();

            int parenCounter = 0;
            int from = 0;
            for (int i = 0; i < message.Length; ++i)
            {
                if (message[i] == '{') parenCounter++;
                else if (message[i] == '}') parenCounter--;

                if (parenCounter == 0)
                {
                    messages.Add(message.Substring(from, i + 1 - from));
                    from = i + 1;
                }
            }

            if (parenCounter != 0)
            {
                return null;
            }

            return messages;
        }

        private static bool PrintAllExceptionsInner(System.Exception e)
        {
            if (e == null) return false;

            if (!PrintAllExceptionsInner(e.InnerException))
            {
                Logger.Log().Error("Caused by {0}: {1}", e.GetType().ToString(), e.Message);
            }
            else
            {
                Logger.Log().Error("...which caused {0}: {1}", e.GetType().ToString(), e.Message);
            }
            Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
            return true;
        }

        /**
         * Prints exception chain until InnerException is null
         */
        public static void PrintAllExceptions(string errorMsg, System.Exception e)
        {
            if (e == null) return;

            Logger.Log().Error(errorMsg);
            PrintAllExceptionsInner(e);
        }

        /**
         * Shortens string @p str to have at most @p max max characters.
         * If the string is too long, it will be shortened to the last space
         * character (if exists) and ellipsis will be added at the end.
         * The @p max limit does NOT include the ellipsis, which will take 3 chars.
         */
        public static string Shorten(string str, int max)
        {
            if (str.Length <= max)
            {
                return str;
            }

            string ret = str.Substring(0, max);

            int lastSpaceIdx = ret.LastIndexOf(' ');
            if (lastSpaceIdx > 0)
            {
                ret = ret.Substring(0, lastSpaceIdx);
            }

            ret += "...";
            return ret;
        }

        /**
         * Capitalizes string @p str so that it always starts with a capital
         * letter (if it starts with a letter).
         *
         * If @p str is null or empty, returns an empty string.
         */
        public static string Capitalize(string str)
        {
            if (String.IsNullOrEmpty(str))
                return String.Empty;

            char[] arr = str.ToCharArray();
            arr[0] = char.ToUpper(arr[0]);
            return new string(arr);
        }

        // Common Config interactions //

        private static Path GetUserModulesPath(string service)
        {
            return Path.Start()
                .Push(service)
                .Push(Constants.PROP_STORE_MODULES_DOMAIN);
        }

        public static void AddUserModuleToConfig(string service, string lbUser)
        {
            ConfUtil.ArrayAppendUnique(GetUserModulesPath(service), lbUser);
        }

        public static string[] GetUserModulesFromConfig(string service)
        {
            string[] users;
            if (!Conf.TryGet<string[]>(GetUserModulesPath(service), out users))
            {
                // Couldn't find the config entry, meaning there is no enabled modules.
                // Not considered an error.
                users = new string[0];
            }

            return users;
        }

        public static void RemoveUserModuleFromConfig(string service, string lbUser)
        {
            ConfUtil.ArrayRemove(GetUserModulesPath(service), lbUser);
        }

        /**
         * Attempts a connection multiple times with increasing sleep timeout in-between.
         * This is used in cases where outer services can sometimes fail to connect
         * for reasons unknown to mankind and retrying is the easiest option. After
         * connection fails for @p attempts times, throws an Exception.
         *
         * @p attempts is the total attempts count before throwing an Exception.
         * @p waitIntervalMs is in milliseconds and doubled between each attempt.
         * @p connectFunc is the connection routine. It can return an object of choice
         * of type T. Throwing an Exception from inside signifies connection failing.
         * Takes one int argument, which is the current attempt counter.
         *
         * If @p connectFunc fails an attempt, it should throw an Exception - it will
         * be caught by the try-catch block of this utility function which will then log
         * the error, sleep for @p waitIntervalMs time interval and retry the connection
         * routine. If this routine exits without throwing it is assumed the connection
         * attempt was successful and we can leave without raising any errors or retrying.
         *
         * Throws: ConnectionFailedException. InnerException will contain the reason for failure
         * that was thrown by @p connectFunc.
         *
         * Example:
         *
         * Assuming @p waitInternal is set to 1000ms (1s) and @p attempts is 4
         * the function will work as follows if all connection attempts fail:
         *    connectFunc() -- fails
         *    wait(waitIntervalMs) -- 1s wait
         *    waitIntervalMs *= 2
         *    connectFunc() -- fails
         *    wait(waitIntervalMs) -- 2s wait
         *    waitIntervalMs *= 2
         *    connectFunc() -- fails
         *    wait(waitIntervalMs) -- 4s wait
         *    waitIntervalMs *= 2
         *    connectFunc() -- fails, throws ConnectionFailedException
         *
         * If any of above connectFunc() attempts succeeds, the utility function exits
         * returning the object returned by @p connectFunc.
         */
        public static async Task<T> TryConnectAsync<T>(int attempts, int waitIntervalMs, Func<int, Task<T>> connectFunc)
        {
            int currentAttempt = 0;
            int currentWaitInterval = waitIntervalMs;

            while (currentAttempt < attempts)
            {
                try
                {
                    return await connectFunc(currentAttempt);
                }
                catch (System.Exception e) when (currentAttempt < (attempts - 1))
                {
                    Logger.Log().Warning("Connection attempt {0} failed, caught {1}. Retrying in {2} seconds...", currentAttempt, e.Message, currentWaitInterval / 1000);
                    Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);

                }
                catch (System.Exception e)
                {
                    throw new ConnectionFailedException(String.Format("Failed to connect after {0} attempts.", attempts), e);
                }

                currentAttempt++;
                if (currentAttempt < attempts)
                {
                    Thread.Sleep(currentWaitInterval);
                    currentWaitInterval *= 2;
                }
            }

            throw new ConnectionFailedException(String.Format("Failed to connect after {0} attempts.", attempts));
        }

        public static T TryConnect<T>(int attempts, int waitIntervalMs, Func<int, T> f)
        {
            Task<T> t = TryConnectAsync(attempts, waitIntervalMs, async (attempts) =>
            {
                return f(attempts);
            });
            t.Wait();
            return t.Result;
        }

        public static async Task TryConnectAsync(int attempts, int waitIntervalMs, Func<int, Task> a)
        {
            await TryConnectAsync<int>(attempts, waitIntervalMs, async (attempts) =>
            {
                await a(attempts);
                return 0;
            });
        }

        public static void TryConnect(int attempts, int waitIntervalMs, Action<int> a)
        {
            Task<int> t = TryConnectAsync<int>(attempts, waitIntervalMs, async (attempts) =>
            {
                a(attempts);
                return 0;
            });
            t.Wait();
        }
    }
}
