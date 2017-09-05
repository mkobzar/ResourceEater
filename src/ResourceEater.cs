using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace ResourceEater
{
    class Program
    {
        static bool haveToEatRAM = false;
        static bool haveToEatHDD = false;
        static bool haveToEatCPU = false;
        static long hddLeaveMb = 8;
        static int maxThreads = 8;    
        static string largeFile = "VeryLargeFile.tmp";
        static string CpuArg = "CPU";
        static string RamArg = "RAM";
        static string HddArg = "HDD";
        static string ProcessPriorityArg = "P";
        public static bool isExiting = false;
        
        /// <summary>
        /// main
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            SetConsoleCtrlHandler(new HandlerRoutine(ExitCheck), true);

            CleanUp();
            GetSettings(args);
            if (!haveToEatCPU && !haveToEatHDD && !haveToEatRAM)
            {
                ShowUsage();
                return;
            }

            Eating();

            while (!isExiting)
            {
                System.Threading.Thread.Sleep(1000);
            }
            DeleteFile(largeFile);
        }

        private static bool ExitCheck(CtrlTypes ctrlType)
        {
            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                case CtrlTypes.CTRL_BREAK_EVENT:
                case CtrlTypes.CTRL_CLOSE_EVENT:
                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    isExiting = true;
                    break;
            }
            return true;
        }

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        /// <summary>
        /// validate is string can be converted to Int32
        /// </summary>
        /// <paRam name="s"></paRam>
        /// <returns></returns>
        static bool isNumeric(string s)
        {
            try
            {
                uint.Parse(s);
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// parse argumentrs and set values for resource eating
        /// </summary>
        /// <paRam name="arguments"></paRam>
        static void GetSettings(string[] arguments)
        {
            try
            {
                {
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        string oneArgument = arguments[i].ToUpper();
                        if(oneArgument.Contains("?"))
                        {
                            ShowUsage();
                            return;
                        }

                        char[] charsToTrim = { '/', '-' };
                        oneArgument = oneArgument.TrimStart(charsToTrim);

                        #region parsing CPU arguments
                        if (oneArgument.StartsWith(CpuArg))
                        {
                            haveToEatCPU = true;
                            try
                            {
                                string s = "";
                                if (oneArgument.Contains("="))
                                {
                                    s = oneArgument.Substring(oneArgument.IndexOf("=") + 1);
                                    if (isNumeric(s))
                                    {
                                        int pri = Convert.ToInt32(s);
                                        switch (pri)
                                        {
                                            case 0:
                                                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
                                                break;
                                            case 1: //no change = Normal
                                                break;
                                            case 2:
                                                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
                                                break;
                                            default:
                                                Console.WriteLine("Entered < " + s + " > Process Priority is not valid, and it won't be changed");
                                                break;
                                        }

                                    }
                                }
                                //is next argument numeric value?
                                else if ((i + 1 < arguments.Length) && isNumeric(arguments[i + 1]))
                                {
                                    hddLeaveMb = long.Parse(arguments[i + 1]);
                                    i++;
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("ignogable Exception happen during parsing HDD arguments, program will contineu to work");
                                Console.WriteLine(e.Message);
                                Console.WriteLine(e.StackTrace);
                            }
                        }
                        #endregion parsing CPU arguments

                        #region parsing RAM arguments
                        else if (oneArgument.StartsWith(RamArg)) 
                        {
                            haveToEatRAM = true;
                        }

                        #endregion parsing RAM arguments
                            
                        #region parsing HDD arguments
                        else if (oneArgument.StartsWith(HddArg)) 
                        {
                            haveToEatHDD = true;
                            try
                            {
                                string s = "";
                                if (oneArgument.Contains("="))
                                {
                                    s = oneArgument.Substring(oneArgument.IndexOf("=") + 1);
                                    if (isNumeric(s))
                                        hddLeaveMb = long.Parse(s);
                                }
                                //is next argument numeric value?
                                else if ((i + 1 < arguments.Length) && isNumeric(arguments[i + 1]))
                                {
                                    hddLeaveMb = long.Parse(arguments[i + 1]);
                                    i++;
                                }
                            }
                            catch { } //ignoring

                            //hddLeaveMb cannot be less then 1mb of free space on HHD)
                            if (hddLeaveMb < 1)
                            {
                                hddLeaveMb = 1;
                            }   
                        }

                        else if (oneArgument.StartsWith(ProcessPriorityArg))
                        {


                            //hddLeaveMb cannot be less when 1mb (cannot leave less then 1mb of free space on HHD)
                            if (hddLeaveMb < 1)
                            {
                                hddLeaveMb = 1;
                            }
                        }
                        #endregion parsing LeaveFreeSpace values
                        
                        else
                        {
                            ShowUsage();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ShowUsage();
                throw new Exception("Input arguments are not valid \n" + e.Message);
            }

        }
       
        /// <summary>
        /// Shows Program Usage
        /// </summary>
        static public void ShowUsage()
        {
            string text = "\nUSAGE:\n\tResourceEater CPU HDD RAM" +
                "\n   ResourceEater is a command-line tool to utilize CPU, RAM, and HDD resources." +
                // "\n   ResourceEater started without arguments will consume max of available resources." +
                "\n   To customize resources consumption specify options for each resource" +
                "\nOPTIONS" + "\n	CPU\t To include max CPU utilization in Normal Priority" +
                "\n	RAM\t Allocate max of available memory" +
                "\n	HDD\t Allocate max (by default max-8mb) of free space on drive where this application running" +
                "\n	HDD=MB\t Allocate max of available memory minus MB you enterd" +
                "\n	CPU\t Utilaze CPU upto 100% with Normal process Priority" +
                "\n	CPU=0\t Set this tool Process Priority" +
            "\n\t\twhere 0 = above normal,  1 = normal, and 2 = below normal";
            Console.WriteLine(text);
        }

        /// <summary>
        /// clean HDD from previous resource consumptions
        /// close all ResourceKiilerd applications
        /// </summary>
        static void CleanUp()
        {
            //kill other ResourceEater if they already running
            //get all ResourceEater.exe's
            Process[] ResourceEaters;
            string thisAppName = AppDomain.CurrentDomain.FriendlyName.Substring(0, AppDomain.CurrentDomain.FriendlyName.Length - 4);
            ResourceEaters = Process.GetProcessesByName(thisAppName);
            foreach (Process oneEater in ResourceEaters)
            {
                if (Process.GetCurrentProcess().Id != oneEater.Id)
                    oneEater.Kill();
            }
            DeleteFile(largeFile);
        }


        static void DeleteFile(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                    Thread.Sleep(500);
                    if (File.Exists(fileName))
                    {
                        throw new Exception("\n Could not delete temp file: " + fileName);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("DeleteFile(" + fileName + ") failed \n" + e.Message + e.StackTrace);
            }
        }


        /// <summary>
        /// run this to downgrade System
        /// this will use agruments which you provided
        /// </summary>
        static void Eating()
        {
            Console.WriteLine("CTRL+C, CTRL+BREAK or suppress the application to exit");
            //1. eat HDD space
            if (haveToEatHDD)
            {
                try
                {
                    //jobNumber++;
                    //Get disk size avaliable 
                    DriveInfo di = new DriveInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()));
                    long spaceAvaliableOnHddMb = di.AvailableFreeSpace;
                    long hddToEatBytes = spaceAvaliableOnHddMb - (long)hddLeaveMb * 1024 * 1024;
                    if (hddToEatBytes > 0 && hddToEatBytes < spaceAvaliableOnHddMb)
                    {
                        using (FileStream fs = new FileStream(largeFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        {
                            fs.Seek(hddToEatBytes, SeekOrigin.Begin);
                            fs.WriteByte(0);
                            fs.Close();
                            Console.WriteLine("Consumed " + (hddToEatBytes / 1024 / 1024 / 1024).ToString() + "GB of HDD space by creating " + fs.Name);
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Could delete and or write new bufferIoFile \n" + e.Message);
                }
            }

            //2. eat  Memory:
            if (haveToEatRAM)
            {
                Thread RamThread;
                RAM RamEater;
                RamEater = new RAM();
                RamThread = new Thread(RamEater.AllocateMem);
                RamThread.Start();
            }
            
            //3. Utilize CPU
            if (haveToEatCPU)
            {
                CPU CpuEater;
                List<Thread> CpuThreads = new List<Thread>();
                try
                {
                    for (int i = 0; i < maxThreads; i++)
                    {
                        CpuEater = new CPU();
                        CpuThreads.Add(new Thread(CpuEater.Run));
                        CpuThreads[i].Start();
                    }
                }
                catch { }
                finally
                {
                    Console.WriteLine(CpuThreads.Count.ToString() + " thread are started to utilaze CPU");
                }
            }

            //4. wait for exit
            for (; ; )
            {
                if (isExiting)
                {
                    return;
                }
                Thread.Sleep(1000);
            }
        }
    }

    public class RAM
    {
        /// <summary>
        /// Allocate max Global memory, refresh allocation every 5 seconds
        /// </summary>
        public void AllocateMem()
        {
            try
            {
                List<IntPtr> pList = new List<IntPtr>();
                int attemptCounter = 0;
                for (; ; )
                {
                    try
                    {
                        attemptCounter++;
                        IntPtr p = new IntPtr();
                        int b = 67108864; //=64MB
                        p = Marshal.AllocHGlobal(b);
                        if (p != IntPtr.Zero)
                        {
                            pList.Add(p);
                        }
                    }
                    catch
                    {
                        if (attemptCounter > 1000)
                        {
                            Console.WriteLine((pList.Count * 64 / 1000).ToString() + "GB (max possible) were allocated in Global//Virtual Memory.");
                            return;
                        }
                    }
                }
            }
            catch { }
        }
    }
    
    public class CPU
    {
        //default CTOR
        public CPU() { }
        /// <summary>
        /// running infinite loop 
        /// </summary>
        public void Run()
        {
            int job = 2;
            while (true)
            {
                if (Program.isExiting)
                {
                    return;
                }
                job = job + 1;
                job = job - 1;
            }
        }
    }
}



