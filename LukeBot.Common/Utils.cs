﻿using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;


namespace LukeBot.Common
{
    public class Utils
    {
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
    }
}
