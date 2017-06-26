//Constructed by referencing https://stackoverflow.com/questions/10520048/calculate-md5-checksum-for-a-file
//Improved referencing http://blog.monogram.sk/pokojny/2011/09/25/calculating-hash-while-processing-stream/

//-quiet to avoid messages, -list for outputting a list of hashes and file paths separated by tabs, -listbare for just hashes, -updatefrequency=seconds
//md5gen.exe PathToFile
//md5gen.exe (-list or -listbare) "PathToListOfFiles" "PathToFileToListMD5s"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Diagnostics;

namespace md5gen
{
    class Program
    {
        static void Main(string[] args)
        {
            //Examining arguments and setting default values
            bool boolQuiet = false;
            bool boolList = false;
            bool boolListBare = false;
            bool boolCanSeek = !Console.IsOutputRedirected;

            TimeSpan tsDelay = new TimeSpan();
            tsDelay = TimeSpan.FromSeconds(0.1);
            string strInputPath = "";
            string strOutputPath = "";

            foreach (string strCurrentArg in args)
            {
                string strTemp = strCurrentArg.ToLower();
                switch (strTemp)
                {
                    case "-quiet":
                        boolQuiet = true;
                        break;

                    case "-list":
                        boolList = true;
                        break;

                    case "-listbare":
                        boolList = true;
                        boolListBare = true;
                        break;

                    default:
                        if ((strTemp.Length > 17) && (strTemp.Substring(0, 17) == "-updatefrequency="))
                        {
                            Double.TryParse(strTemp.Substring(17, (strTemp.Length - 17)), out double dblTemp);
                            if (dblTemp > 0)
                            {
                                tsDelay = TimeSpan.FromSeconds(dblTemp);
                            }
                        }
                        else if (strInputPath.Length == 0)
                        {
                            strInputPath = strCurrentArg;
                        }
                        else if ((strOutputPath.Length == 0) && boolList)
                        {
                            strOutputPath = strCurrentArg;
                            if (File.Exists(strOutputPath))
                            {
                                Console.WriteLine(strOutputPath + " already exists, aborting.");
                                System.Environment.Exit(80);
                            }
                        }
                        break;
                }
            }

            try
            {
                if ((strInputPath.Length > 0) && (strOutputPath.Length == 0))
                {
                    Console.WriteLine(GetMD5(strInputPath, tsDelay, boolQuiet));
                }
                else if (boolList)
                {
                    //These will throw an exception if the paths are invalid.
                    Path.GetFullPath(strInputPath);
                    Path.GetFullPath(strOutputPath);
                    Path.GetFileName(strInputPath);
                    Path.GetFileName(strOutputPath);

                    string strTempLineRead;
                    string strTempMD5;
                    string strTempLineWrite;

                    //No StreamWriter constructor accepts a path while specifying encoding, hence the nested "new FileStream..." here
                    //Learned from https://stackoverflow.com/a/8151412
                    //Without Unicode, many possible file paths will not be processed correctly.  Redirected cmd output when cmd is opened with /u is unicode.
                    //A dir command redirected to a file for output without cmd being opened with /u initially will not accuratedly record file paths with some characters.
                    //Actual typed or pasted input to a cmd console doesn't seem to require the cmd being opened with /u.
                    using (StreamReader strReader = new StreamReader(strInputPath, Encoding.Unicode))
                    using (StreamWriter strWriter = new StreamWriter(new FileStream(strOutputPath, FileMode.CreateNew), Encoding.Unicode))
                    {

                        Stopwatch stpMain = new Stopwatch();
                        TimeSpan tsCheck = new TimeSpan();
                        stpMain.Start();

                        long lngTally = 0;
                        long lngTotalLines = File.ReadLines(strInputPath).Count();
                        int intCursorStart;
                        if (boolCanSeek)
                        {
                            intCursorStart = Console.CursorTop + 1;
                        }
                        else
                        {
                            intCursorStart = -1;
                        }

                        while (strReader.EndOfStream == false)
                        {
                            strTempLineRead = strReader.ReadLine();
                            lngTally = lngTally + 1;
                            if ((!boolQuiet && (lngTally == 1 || (stpMain.Elapsed - tsCheck) >= tsDelay)))
                            {

                                BackCursorUp(intCursorStart);
                                Console.WriteLine($"Processing List: {lngTally}/{lngTotalLines}");
                                tsCheck = stpMain.Elapsed;
                            }

                            if (strTempLineRead == "")
                            {
                                strTempLineWrite = "";
                            }
                            else
                            {
                                strTempMD5 = GetMD5(strTempLineRead, tsDelay, boolQuiet, lngTally, lngTotalLines, intCursorStart);
                                if (boolListBare == true)
                                {
                                    strTempLineWrite = strTempMD5;
                                }
                                else
                                {
                                    strTempLineWrite = strTempLineRead + "\t" + strTempMD5;
                                }
                            }
                            strWriter.WriteLine(strTempLineWrite);



                        }
                        stpMain.Stop();
                        if (!boolQuiet)
                        {
                            BackCursorUp(intCursorStart);
                            Console.WriteLine($"Finished processing {lngTally} lines in: {stpMain.Elapsed}");
                        }
                    }

                }
                else
                {
                    Console.WriteLine("Invalid arguments.");
                }
            }
            catch (Exception Ex)
            {
                Console.WriteLine("Something went wrong using the specified arguments. - Exception: " + Ex.Message.Replace('\r', ' ').Replace('\n', ' '));
            }
        }

        //Function to return hexadecimal MD5 hash
        static public string GetMD5(string strInputPath, TimeSpan tsDelay, bool boolBeQuiet = false, long lngTally = 0, long lngTotalLines = 0, int intTallyCurStart = -1)
        {
            try
            {
                //Utilizing block method since it's impossible to display progress on large files otherwise.
                //"using" here lets the compiler definitively know to dispose of the objects used when out of scope.
                //FileShare.ReadWrite is very important so other programs are not blocked from what they were doing with their files.
                using (FileStream streamFile = File.Open(strInputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (MD5 md5 = MD5.Create())
                using (CryptoStream cs = new CryptoStream(streamFile, md5, CryptoStreamMode.Read))
                {
                    int byteCount;
                    byte[] data = new byte[4096];
                    string strPath = string.Empty;
                    string strName = string.Empty;
                    decimal decPosition = 0;
                    string strPosition = string.Empty;
                    decimal decSize = 0;
                    string strSize = string.Empty;
                    string strPercent = string.Empty;

                    bool boolCanSeek = !Console.IsOutputRedirected;

                    Stopwatch stpMD5 = new Stopwatch();
                    stpMD5.Start();

                    TimeSpan tsMD5Check = new TimeSpan();



                    decSize = ((decimal)streamFile.Length);

                    strPath = Path.GetDirectoryName(streamFile.Name);
                    strName = Path.GetFileName(streamFile.Name);
                    decPosition = 0;
                    strPosition = "0";
                    strSize = (decSize / 1048576).ToString("0");
                    strPercent = "0";
                    bool boolHasNewLine = false;
                    int intConsolePosition = -1;

                    while ((byteCount = cs.Read(data, 0, data.Length)) > 0)
                    {

                        if (!boolBeQuiet && ((stpMD5.Elapsed - tsMD5Check) >= tsDelay))
                        {
                            decPosition = streamFile.Position;
                            strPosition = (decPosition / 1048576).ToString("0");
                            strPercent = (100 * (decPosition / decSize)).ToString("0.00");

                            if (boolHasNewLine == false)
                            {
                                BackCursorUp(intTallyCurStart);
                                Console.WriteLine($"Processing List: {lngTally}/{lngTotalLines}");

                                boolHasNewLine = true;
                                if (boolCanSeek)
                                {
                                    intConsolePosition = Console.CursorTop + 1;
                                }
                            }

                            ProgressUpdate(strPath, strName, strPosition, strSize, strPercent, intConsolePosition);



                            tsMD5Check = stpMD5.Elapsed;
                        }
                    }

                    if (!boolBeQuiet && (stpMD5.Elapsed > tsDelay))
                    {
                        if (boolHasNewLine == false)
                        {
                            BackCursorUp(intTallyCurStart);
                            Console.WriteLine($"Processing List: {lngTally}/{lngTotalLines}");

                            if (boolCanSeek)
                            {
                                intConsolePosition = Console.CursorTop + 1;
                            }
                        }
                        ProgressUpdate(strPath, strName, strSize, strSize, "100", intConsolePosition);
                    }

                    byte[] bytMD5 = md5.Hash;

                    return BitConverter.ToString(bytMD5).Replace("-", String.Empty).ToLower();
                }
            }
            catch (Exception Ex)
            {
                return "UnableToRead - Exception: " + Ex.Message.Replace('\r', ' ').Replace('\n', ' ');
            }
        }

        static public void ProgressUpdate(string strPath, string strName, string strPosition, string strSize, string strPercent, int intUseConsolePosition = -1)
        {
            BackCursorUp(intUseConsolePosition);
            Console.WriteLine($"MD5 Hash Progress ({strPercent}%): {strPosition}/{strSize}MB {strName} ~ Location: {strPath}");
        }
        static public void BackCursorUp(int intPosition = -1)
        {
            if (!Console.IsOutputRedirected)
            {
                int intCurTop = Console.CursorTop;
                if ((intPosition > -1) && (intPosition <= intCurTop))
                {
                    while (intCurTop >= intPosition)
                    {
                        Console.Write(new string(' ', Console.BufferWidth));
                        intCurTop--;
                        Console.SetCursorPosition(0, intCurTop);
                    }
                }
            }
        }

    }
}
