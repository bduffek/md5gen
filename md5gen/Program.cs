//Constructed by referencing https://stackoverflow.com/questions/10520048/calculate-md5-checksum-for-a-file
//Improved referencing http://blog.monogram.sk/pokojny/2011/09/25/calculating-hash-while-processing-stream/

//-quiet to avoid messages, -list for outputting a list of hashes and file paths separated by tabs, -listbare for just hashes
//md5gen.exe PathToFile
//md5gen.exe (-list or -listbare) "PathToListOfFiles" "PathToFileToListMD5s"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace md5gen
{
    class Program
    {
        static void Main(string[] args)
        {
            //Examining arguments
            bool boolQuiet = false;
            bool boolList = false;
            bool boolListBare = false;
            string strInputPath = "";
            string strOutputPath = "";

            int intIndex;
            for (intIndex = 0; intIndex < args.Count(); intIndex++)
            {
                string strTemp = args[intIndex].ToLower();
                switch (strTemp)
                {
                    case "-quiet":
                        boolQuiet = true;
                        break;

                    case "-list":
                        boolList = true;
                        break;

                    case "-listbare":
                        boolListBare = true;
                        break;

                    default:
                        if (strInputPath.Length == 0)
                        {
                            strInputPath = args[intIndex];
                        }
                        else if (strOutputPath.Length == 0)
                        {
                            strOutputPath = args[intIndex];
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
                    Console.WriteLine(GetMD5(strInputPath, boolQuiet));
                }
                else if ((boolList == true) || (boolListBare == true))
                {
                    //These will throw an exception if the paths are invalid.
                    Path.GetFullPath(strInputPath);
                    Path.GetFullPath(strOutputPath);
                    Path.GetFileName(strInputPath);
                    Path.GetFileName(strOutputPath);

                    string strTempLineRead;
                    string strTempMD5;
                    string strTempLineWrite;

                    using (StreamReader strReader = new StreamReader(strInputPath))
                    using (StreamWriter strWriter = new StreamWriter(strOutputPath))
                    {
                        long lngTally = 0;
                        long lngTotalLines = File.ReadLines(strInputPath).Count();
                        while (strReader.EndOfStream == false)
                        {
                            strTempLineRead = strReader.ReadLine();

                            if (strTempLineRead == "")
                            {
                                strTempLineWrite = "";
                            }
                            else
                            { 
                                strTempMD5  = GetMD5(strTempLineRead, boolQuiet);
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
                            
                            lngTally = lngTally + 1;
                        //Updating console only at 511 and then every 511 lines via bitwise "and" use.
                        //Updating the console slows the program down very dramatically otherwise, if doing small files.
                        //The conditions here mean as long as -quiet wasn't used, updats will occur if the total lines is less than 511, every 511 lines,
                        //or if the current file is more than about 20MB.

                            long lngSizeHolder;

                            if (File.Exists(strTempLineRead))
                            {
                                lngSizeHolder = (new FileInfo(strTempLineRead).Length);
                            }
                            else
                            {
                                lngSizeHolder = 0;
                            }

                            if ((!boolQuiet && (((lngTally & 511) == 511)) || (lngTotalLines < 511) || (lngSizeHolder > 20971520)))
                            {
                                Console.WriteLine($"Processing List: {lngTally}/{lngTotalLines}");
                            }
                        }
                        if (!boolQuiet)
                        {
                            Console.WriteLine($"Finished processing {lngTally} lines.");
                        }
                    }

                }
                else
                {
                    Console.WriteLine("Invalid arguments.");
                }
            }
            catch
            {
                Console.WriteLine("Something went wrong using the specified arguments.");
            }
        }

        //Function to return hexadecimal MD5 hash
        static public string GetMD5(string strInputPath, bool boolBeQuiet = false)
        {
            try
            {
                //Utilizing block method since it's impossible to display progress on large files otherwise.
                //"using" here lets the compiler definitively know to dispose of the objects used when out of scope.
                //FileShare.ReadWrite is very important so other programs are not blocked from what they were doing with their files.
                using (FileStream streamFile = File.Open(strInputPath,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
                using (MD5 md5 = MD5.Create())
                using (CryptoStream cs = new CryptoStream(streamFile, md5, CryptoStreamMode.Read))
                {
                    int byteCount;
                    byte[] data = new byte[4096];
                    while ((byteCount = cs.Read(data, 0, data.Length)) > 0)
                    {
                        if (!boolBeQuiet && (((streamFile.Position - 1) & 67108863) == 67108863))
                        {
                            Console.WriteLine("MD5 Hash Progress (" + Path.GetFileName(streamFile.Name) + "): " +
                                             ((decimal)streamFile.Position/1048576).ToString("0") + "/" +
                                             ((decimal)streamFile.Length/1048576).ToString("0") + "MB " +
                                             (100 * ((decimal)streamFile.Position/(decimal)streamFile.Length)).ToString("0.00") +
                                             "% ~ Location: " + Path.GetDirectoryName(streamFile.Name));
                        }
                    }

                    if (!boolBeQuiet && (streamFile.Length > 67108863))
                    {
                        Console.WriteLine("MD5 Hash Progress (" + Path.GetFileName(streamFile.Name) + "): " +
                                         ((decimal)streamFile.Position / 1048576).ToString("0") + "/" +
                                         ((decimal)streamFile.Length / 1048576).ToString("0") + "MB " +
                                         (100 * ((decimal)streamFile.Position / (decimal)streamFile.Length)).ToString("0.00") +
                                         "% ~ Location: " + Path.GetDirectoryName(streamFile.Name));
                    }

                    byte[] bytMD5 = md5.Hash;

                    return BitConverter.ToString(bytMD5).Replace("-", String.Empty).ToLower();
                }
            }
            catch
            {
                return "UnableToRead";
            }
        }
    }
}

