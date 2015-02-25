﻿/*
    This file is part of HomeGenie Project source code.

    HomeGenie is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    HomeGenie is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with HomeGenie.  If not, see <http://www.gnu.org/licenses/>.  
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: http://homegenie.it
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MIG.Gateways
{
    public class WebServiceUtility
    {


        public static void WriteStringToContext(System.Net.HttpListenerContext context, string returnValue)
        {
            Encoding encoding = context.Response.ContentEncoding;
            if (encoding == null) encoding = Encoding.GetEncoding("ISO-8859-1");
            WriteBytesToContext(context, encoding.GetBytes(returnValue));
        }
        public static void WriteBytesToContext(System.Net.HttpListenerContext context, byte[] buffer)
        {
            try
            {
                context.Response.ContentLength64 = buffer.Length;
                System.IO.Stream output = context.Response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
            catch
            {
                // TODO: add error logging 
            }
        }


        // code adapted from
        // http://stackoverflow.com/questions/8466703/httplistener-and-file-upload

        public static String GetBoundary(String contentType)
        {
            if (contentType == null) return "";
            return /*"--" + */ contentType.Split(';')[1].Split('=')[1];
        }

        public static void SaveFile(Encoding encoding, String boundary, Stream input, string outputFile)
        {
            Byte[] boundaryBytes = new byte[] { }; if (boundary != null) boundaryBytes = encoding.GetBytes(boundary);
            Int32 boundaryLen = boundaryBytes.Length;

            using (MemoryStream output = new MemoryStream())
            {
                Byte[] buffer = new Byte[1024];
                Int32 len = input.Read(buffer, 0, 1024);
                Int32 startPos = -1;

                //// Find start boundary
                while (true)
                {
                    if (len == 0 && boundary != null)
                    {
                        throw new Exception("Start Boundary Not Found");
                    }

                    startPos = IndexOf(buffer, len, boundaryBytes);
                    if (startPos >= 0)
                    {
                        break;
                    }
                    else
                    {
                        Array.Copy(buffer, len - boundaryLen, buffer, 0, boundaryLen);
                        len = input.Read(buffer, boundaryLen, 1024 - boundaryLen);
                    }
                }

                // Skip four lines (Boundary, Content-Disposition, Content-Type, and a blank)
                for (Int32 i = 0; i < 4; i++)
                {
                    while (true)
                    {
                        if (len == 0)
                        {
                            throw new Exception("Preamble not Found.");
                        }

                        startPos = Array.IndexOf(buffer, encoding.GetBytes("\n")[0], startPos);
                        if (startPos >= 0)
                        {
                            startPos++;
                            break;
                        }
                        else
                        {
                            len = input.Read(buffer, 0, 1024);
                        }
                    }
                }

                Array.Copy(buffer, startPos, buffer, 0, len - startPos);
                len = len - startPos;

                while (true)
                {
                    Int32 endPos = IndexOf(buffer, len, boundaryBytes);
                    if (endPos > 0)
                    {
                        output.Write(buffer, 0, endPos);
                        break;
                    }
                    else if (len <= boundaryLen)
                    {
                        throw new Exception("End Boundary Not Found");
                    }
                    else
                    {
                        output.Write(buffer, 0, len - boundaryLen);
                        Array.Copy(buffer, len - boundaryLen, buffer, 0, boundaryLen);
                        len = input.Read(buffer, boundaryLen, 1024 - boundaryLen) + boundaryLen;
                    }
                }
                output.Seek(0, SeekOrigin.Begin);
                long ps = FindPosition(output, encoding.GetBytes("\n--"));
                //startPos = Array.IndexOf(output.ToArray(), enc.GetBytes("\n---"), 0);
                if (ps > 0)
                {
                    byte[] b = output.ToArray();
                    FileStream fs = new FileStream(outputFile, FileMode.Create);
                    fs.Write(b, 0, (int)ps - 1);
                    fs.Close();
                    //Array.Resize(
                }
            }
        }


        public static long FindPosition(Stream stream, byte[] byteSequence)
        {
            if (byteSequence.Length > stream.Length)
                return -1;

            byte[] buffer = new byte[byteSequence.Length];

            using (BufferedStream bufStream = new BufferedStream(stream, byteSequence.Length))
            {
                while ((bufStream.Read(buffer, 0, byteSequence.Length)) == byteSequence.Length)
                {
                    if (byteSequence.SequenceEqual(buffer))
                        return bufStream.Position - byteSequence.Length;
                    else
                        bufStream.Position -= byteSequence.Length - PadLeftSequence(buffer, byteSequence);
                }
            }

            return -1;
        }

        private static int PadLeftSequence(byte[] bytes, byte[] seqBytes)
        {
            int i = 1;
            while (i < bytes.Length)
            {
                int n = bytes.Length - i;
                byte[] aux1 = new byte[n];
                byte[] aux2 = new byte[n];
                Array.Copy(bytes, i, aux1, 0, n);
                Array.Copy(seqBytes, aux2, n);
                if (aux1.SequenceEqual(aux2))
                    return i;
                i++;
            }
            return i;
        }


        private static Int32 IndexOf(Byte[] buffer, Int32 len, Byte[] boundaryBytes)
        {
            for (Int32 i = 0; i <= len - boundaryBytes.Length; i++)
            {
                Boolean match = true;
                for (Int32 j = 0; j < boundaryBytes.Length && match; j++)
                {
                    match = buffer[i + j] == boundaryBytes[j];
                }

                if (match)
                {
                    return i;
                }
            }

            return -1;
        }


    }
}
