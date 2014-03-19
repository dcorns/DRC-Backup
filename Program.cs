using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Net.Mail;
namespace DRCBackup
{
    class Program
    {
        #region Definitions--------------------------------------------------------------------

        public static string logpath = "";//set from ini in line 246
        public static string logfile = "";//set from ini in line 251
        public static string sumpath = @"c:\DRCTECH\BKSummary.log";//Permenently assigned here
        public static bool BackupSysState = false;//Set from ini in line 252
        public static long bfiles = 0;//number of files backed up
        public static long dircount = 1;//number of directories checked
        public static long filecount = 0;//number of files checked
        public static string ErrorMsg = "";//Used for more messages than errors
        public static int ErrorCode = 0;//Used to avoid certain opperation under error conditions
        public static string emailhost = "";//Aassigned from ini in 196
        public static int emailport = 587;//Assigned from ini in 197
        public static string emailreturnaddr = "";//Assigned from ini in 198
        public static List<string> BPaths = new List<string>();//Contains source paths assigned from ini in 229
        public static List<string> BKD = new List<string>();//Contains backup devices assigned from ini in 202
        public static List<string> BKDAYS =new List<string>();//Contains days of rotation for each backup device and assigned from ini in 210
        public static List<string> emailrecipients = new List<string>();//Contains notification email address and assigned from ini in 195
        public static string BKType = "";//Hold D or F for the backup type F=Full and D=Differential assigned from BKTypeList[] in 249
        public static int usedev = 0;//Holds device to be used and assigned in 317 (a -1 = no device)
        public static string BKDevice = "";//used but never defined
        public static int missdev = -1;//assigned from mrk in 285
        public static int missdays = -1;//assigned from mrk in 287
        public static int lastdev = -1;//assigned from mrk in 277
        public static int rotationdev = 0;//assigned from mrk in 279
        public static int dtimeleft = 0;//assigned from mrk in 281
        public static int BKDCount = 0;//used never assigned
        public static long aspace = 0;//available space on backup device, assigned in 454
        public static long rspace = 0;//space required for backup assigned in 
        public static string targetdir = "";
        public static int lastlognum = -1;//-1 no last log since first time or reset of mark file 479
        public static int loglife = 0;//Number of logfiles to retain before over writing assined from ini in 237
        public static int lognum = 0;//number to be appended to the log filename assigned in 239
        public static int failures = 0;//number of backup failures assigned from mrk in 287
        public static string bdrivename = ""; //given value of bkdmap in 384
        public static string inipath = @"c:\DRCTECH\DrcBackupSys.ini";
        public static string markerpath = @"c:\DRCTECH\DrcBackupSys.mrk";
        public static Configurator con = new Configurator();
        public static int UseDriveStatus = 0;//0=Undefined(no drive available) 1=Correct per rotation 2=Out of Rotation
        public static List<string> JobInfo = new List<string>();
        public static string bkdmap = "";//holds backup device path in 350 or volume label in 373
        public static string netdrive = "";//Drive letter for mapping to network share used as backup device assigned in 351
        public static bool ranconfig = false;//assigned in 157
        public static int sysstatesize = 1000000;//assigned from ini in 244
        #endregion
        static void Main(string[] args)
        {
            DateTime StartTime = new DateTime(); DateTime EndTime = new DateTime();
            #region Default Files Setup
            //Check for required directory DRCTECH. If it does not exist, create it and required files. NOTE: MAKE SURE TO DELETE THIS DIRECTORY IF IT EXIST ALREADY IN ORDER TO CREATE ALL FILES WITHOUT AN ERROR
            if (!(Directory.Exists(@"c:\DRCTECH")))
            {
                Directory.CreateDirectory(@"c:\DRCTECH");

                File.Create(@"c:\DRCTECH\BKSummary.log");
                File.Create(@"c:\DRCTECH\DrcBackupSys.ini");
                File.Create(@"c:\DRCTECH\DrcBackupSys.mrk");
                Console.WriteLine(@"DRCBackup Required Files created, please restart application");
                Console.WriteLine(@"Press Enter To Exit");
                Console.ReadLine();

            }

            else
            {
                //Check size of ini and if less than 1, create default configuration
                FileInfo iconfig = new FileInfo(@"c:\DRCTECH\DrcBackupSys.ini");
                if (iconfig.Length < 1)
                {
                    StreamWriter wconfig = new StreamWriter(@"c:\DRCTECH\DrcBackupSys.ini");
                    wconfig.WriteLine(@"<EMAIL>");
                    wconfig.WriteLine(emailhost);
                    wconfig.WriteLine(emailport);
                    wconfig.WriteLine(emailreturnaddr);
                    wconfig.WriteLine(@"<END>");
                    wconfig.WriteLine(@"<EMAILRECIPIENTS>");
                    wconfig.WriteLine(@"dcorns@drctech.com");
                    wconfig.WriteLine(@"<END>");
                    wconfig.WriteLine(@"<BACKUP DEVICES>");
                    wconfig.WriteLine(@"<END>");
                    wconfig.WriteLine(@"<DEVDAYS>");
                    wconfig.WriteLine(@"<END>");
                    wconfig.WriteLine(@"<PATHS>");
                    wconfig.WriteLine(@"<END>");
                    wconfig.WriteLine(@"<JOBS>");
                    wconfig.WriteLine(logpath);
                    wconfig.WriteLine("7");
                    wconfig.WriteLine(BackupSysState);
                    wconfig.WriteLine(sysstatesize);
                    wconfig.WriteLine(@"<END>");
                    wconfig.WriteLine(@"<DAILY>");
                    wconfig.WriteLine("D");
                    wconfig.WriteLine("D");
                    wconfig.WriteLine("D");
                    wconfig.WriteLine("D");
                    wconfig.WriteLine("F");
                    wconfig.WriteLine("D");
                    wconfig.WriteLine("D");
                    wconfig.WriteLine(@"<END>");
                    wconfig.Close();
                    wconfig.Dispose();
                }
                //Check size of mrk and if less than 1, create default marks
                if(File.Exists("c:\\DRCTECH\\DrcBackupSys.mrk"))
                {
                FileInfo imark = new FileInfo(@"c:\DRCTECH\DrcBackupSys.mrk");
                if (imark.Length < 1)
                {
                    con.MakeNewMarkFile();
                }
                }
                else
                {
                    con.MakeNewMarkFile();
                }
            #endregion

                #region Loop For Configuration options
                Console.WriteLine("Press c to start configuration utility");
                ConsoleKeyInfo Pressed = new ConsoleKeyInfo();
                int countdown = 9;
                int beepincrement = 0;
                string keypressed = "";
                for (int i = 0; i < 100000; i++)
                {
                    if (i == beepincrement)
                    {
                        beepincrement = beepincrement + 10000;
                        Console.CursorLeft = 4;
                        Console.Write(countdown);
                        countdown--;
                        Console.Beep();
                    }

                    if (Console.KeyAvailable)
                    {
                        Pressed = Console.ReadKey();
                        keypressed = Pressed.KeyChar.ToString().ToUpper();
                        break;
                    }
                }
                if (keypressed == "C")
                {
                    con.runconfig();
                    ranconfig = true;

                }


                if (!(ranconfig))
                {
                    Console.Clear();
                    

                #endregion

                    #region Startup Text
                    
                        StreamWriter BKSummary = new StreamWriter(@"c:\DRCTECH\BKSummary.log");
                        System.Console.ForegroundColor = System.ConsoleColor.Cyan;
                        BKSummary.WriteLine(@"This report generated by:");
                        BKSummary.WriteLine(@"Custom Backup Software designed by DRC Technologies");
                        BKSummary.WriteLine(@"Copyright 2009, All rights reserved, DRC Technologies, LLC");
                        BKSummary.WriteLine(@"DRCTECH.BIZ  425.879.7020" + BKSummary.NewLine);
                        BKSummary.Close();
                        BKSummary.Dispose();
                        Console.WriteLine(@"Custom Backup Software designed by DRC Technologies (DRCBackup Ver3)");
                        Console.WriteLine(@"Copyright 2009, All rights reserved, DRC Technologies, LLC");

                    #endregion

                        #region Read Email configuration
                        List<string> emailconf = con.readsection("<EMAIL>");
                        emailhost = emailconf[0];
                        emailport = Convert.ToInt32(emailconf[1]);
                        emailreturnaddr = emailconf[2];
                        emailrecipients = con.readsection("<EMAILRECIPIENTS>");

                        #endregion

                        #region Read/Check and require Target Locations
                        BKD = con.readsection("<BACKUP DEVICES>");

                        //Make Sure at least one device exists
                        if (BKD.Count < 1)
                        {
                            //No Devices, Go to Device config

                            BKD = con.inputsection("<BACKUP DEVICES>");
                            con.writedevices(BKD);
                            //Set days for each device
                            BKDAYS = con.inputsection("<DEVDAYS>");
                            con.resetmark();

                        }
                        BKD = con.readsection("<BACKUP DEVICES>");
                        BKDAYS = con.readdevdays();
                        if (BKDAYS.Count != BKD.Count)
                        {
                            BKDAYS = con.inputsection("<DEVDAYS>");
                            con.resetmark();
                        }
                        #endregion

                        #region Read/Require Backup Paths



                        BPaths = con.readsection("<PATHS>");

                        if (BPaths.Count < 1)//No locations to backup
                        {
                            BPaths = con.inputpaths(BPaths);
                            con.writesection(BPaths, "<PATHS>");
                        }
                        #endregion
                        Console.WriteLine("Proccessing backup job information");
                        #region Proccess Backup Job Information
                        JobInfo = con.readsection("<JOBS>");
                        if (JobInfo[0] == "")
                        {
                            JobInfo = con.inputsection("<JOBS>");
                        }
                        logpath = JobInfo[0];
                        loglife = Convert.ToInt32(JobInfo[1]);
                        if (lastlognum >= loglife) lastlognum = 0;
                        lognum = lastlognum + 1;
                        File.WriteAllText(logpath + DateTime.Now.DayOfWeek.ToString() + lognum + @".log", "Backup Job Proccessed: " + DateTime.Now.ToString());
                        logfile = logpath + DateTime.Now.DayOfWeek.ToString() + lognum + @".log";
                        BackupSysState = Convert.ToBoolean(JobInfo[2]);
                        sysstatesize = Convert.ToInt32(JobInfo[3]);
                        List<string> BKTypeList = con.readsection("<DAILY>");
                        switch (DateTime.Now.DayOfWeek.ToString())
                        {
                            case "Monday":
                                BKType = BKTypeList[0];
                                break;
                            case "Tuesday":
                                BKType = BKTypeList[1];
                                break;
                            case "Wednesday":
                                BKType = BKTypeList[2];
                                break;
                            case "Thursday":
                                BKType = BKTypeList[3];
                                break;
                            case "Friday":
                                BKType = BKTypeList[4];
                                break;
                            case "Saturday":
                                BKType = BKTypeList[5];
                                break;
                            case "Sunday":
                                BKType = BKTypeList[6];
                                break;
                            default:
                                break;
                        }
                        #endregion
                        Console.WriteLine("Proccessing Markers");
                        #region Read and proccess markers
                        StreamReader marker = new StreamReader(markerpath);//Tracks Backup Program History
                        
                        lastdev = Convert.ToInt32(marker.ReadLine());
                        Console.WriteLine("lastdev=" + lastdev);
                        rotationdev = Convert.ToInt32(marker.ReadLine());
                        Console.WriteLine("rotationdev=" + rotationdev);
                        dtimeleft = Convert.ToInt32(marker.ReadLine());
                        Console.WriteLine("dtimeleft=" + dtimeleft);
                        lastlognum = Convert.ToInt32(marker.ReadLine());
                        Console.WriteLine("lastlognum=" + lastlognum);
                        missdev = Convert.ToInt32(marker.ReadLine());
                        Console.WriteLine("missdev=" + missdev);
                        missdays = Convert.ToInt32(marker.ReadLine());
                        Console.WriteLine("missdays=" + missdays);
                        failures = Convert.ToInt32(marker.ReadLine());
                        Console.WriteLine("failures=" + failures);


                        if (dtimeleft < 1)
                        {
                            rotationdev++;//no more days left in rotation select next device in rotation
                            if (rotationdev >= BKD.Count)//Last device was last in rotation, select first backup device.
                            {
                                rotationdev = 0;

                            }
                            dtimeleft = Convert.ToInt32(BKDAYS[rotationdev]);//Set rotation days to number specified for device.
                            missdays = 0;
                            missdev = -5;
                            failures = 0;
                        }

                        marker.Close();
                        marker.Dispose();

                        #endregion
                        Console.WriteLine("Selecting Device");
                        #region Select scheduled BKD or next available BKD
                        usedev = con.getdrive(BKD, rotationdev);

                        if (usedev != rotationdev)
                        {
                            //Rotation drive offline
                            Console.WriteLine(usedev + @" offline");
                            if (usedev != -1)
                            {
                                //Alternate Drive Selected
                                Console.WriteLine("Choosing Alternate Device");
                                if (lastdev < 0) UseDriveStatus = 3; //First time using an alternate device
                                else UseDriveStatus = 2;// not the first time using an alternate device
                            }
                            else
                            {
                                //No Drives Available, backup failure
                                Console.WriteLine("No Devices Available for Backup, Backup Failed");
                                File.AppendAllText(sumpath, Environment.NewLine + @"No Devices Available for Backup, Backup Failed");
                                File.AppendAllText(logfile, Environment.NewLine + @"No Devices Available for Backup, Backup Failed");
                                UseDriveStatus = 0;
                            }
                        }
                        else
                        {
                            //Rotation drive online
                            Console.WriteLine(usedev + @" online");
                            UseDriveStatus = 1;
                        }
                        #endregion
                        if (UseDriveStatus != 0)
                        {
                            Console.WriteLine("Determining whether target is path or label: Device=" + BKD[usedev] + @" usedev=" + usedev);

                            #region Determine whether target is a path or volume label

                            if (BKD[usedev].Contains(@"\"))
                            {
                                //path
                                Console.WriteLine(@"Device is a Path " + BKD[usedev]);
                                bkdmap = BKD[usedev];
                                netdrive = con.findnetdrive();
                                System.Diagnostics.Process mapnet = new System.Diagnostics.Process();
                                mapnet.StartInfo.FileName = "net.exe";
                                mapnet.StartInfo.Arguments = @" use " + netdrive + @": " + BKD[usedev];
                                mapnet.Start();
                                mapnet.WaitForExit();
                                bkdmap = netdrive + @":\";

                            }
                            else
                            {
                                //volume label
                                Console.WriteLine(@"Device is a volume label " + BKD[usedev]);
                                Console.WriteLine(@"BKD[usedev]=" + BKD[usedev]);

                                foreach (DriveInfo di in DriveInfo.GetDrives())
                                {
                                    try
                                    {
                                        Console.WriteLine(di.Name + @" " + di.VolumeLabel + @" present");
                                        if (di.VolumeLabel == BKD[usedev])
                                        {
                                            bkdmap = di.Name;
                                        }
                                        Console.WriteLine("bkdmap=" + bkdmap + @" di.Name=" + di.Name);
                                    }
                                    catch (Exception lex)
                                    {
                                        Console.WriteLine(lex.Message);
                                    }
                                }

                            }
                            bdrivename = bkdmap;
                            Console.WriteLine(@"bdrivename= " + bkdmap);
                            #endregion
                            Console.WriteLine("Checking Device Rotation Status");
                            #region Check device rotation status and handle
                            if (!(missdev == -5))//Device is out of rotation check to see if the device has any days left and if its plugged in
                            //set it to be used for the rest of its rotation
                            {
                                switch (UseDriveStatus)
                                {
                                    case 1:
                                        ErrorMsg = BKDevice + @" back online. Resuming normal sequence.";
                                        Console.WriteLine(BKDevice + @" back online. Resuming normal sequence.");
                                        logerrors(ErrorMsg);
                                        missdev = -5;
                                        break;
                                    case 2:

                                        ErrorMsg = Environment.NewLine + "Device " + BKDevice + " is still not online. It has " + (dtimeleft - 1) + " sequences left.";
                                        Console.WriteLine("Device " + BKDevice + " is still not online. It has " + (dtimeleft - 1) + " sequences left.");
                                        logerrors(ErrorMsg);

                                        if (BKD[lastdev] == BKDevice)
                                        {
                                            ErrorMsg = Environment.NewLine + @"Backups will continue to occur on " + BKD[lastdev] + @" until " + BKD[rotationdev] + " is returned or its sequence expires";
                                            Console.WriteLine(@"Backups will continue to occur on " + BKD[lastdev] + @" until " + BKD[rotationdev] + " is returned or its sequence expires");
                                            missdev = rotationdev;
                                            missdays++;
                                            File.AppendAllText(sumpath, Environment.NewLine + "WARNING, LEAVING A NEWER BACKUP DEVICE ONLINE AND OUT OF ROTATION WILL MAKE IT VULNERABLE TO PREMATURE FILE DELETION SHOULD SPACE BE REQUIRED TO COMPLETE THE NEXT BACKUP" + Environment.NewLine);
                                        }
                                        else
                                        {
                                            ErrorMsg = Environment.NewLine + @"Device " + BKD[lastdev] + @" which was the last device used to replace expected device " + BKD[rotationdev] + @" is no longer online. A new alternate (" + BKDevice + @") has been selected";
                                            Console.WriteLine(@"Device " + BKD[lastdev] + @" which was the last device used to replace expected device " + BKD[rotationdev] + @" is no longer online. A new alternate (" + BKDevice + @") has been selected");
                                            missdev = lastdev;

                                        }
                                        logerrors(ErrorMsg);
                                        break;
                                    case 0:
                                        ErrorMsg = Environment.NewLine + "No sutible device available for this sequence, backup failed" + Environment.NewLine;
                                        Console.WriteLine("No sutible device available for this sequence, backup failed" + Environment.NewLine);
                                        logerrors(ErrorMsg);
                                        ErrorCode = 1;
                                        failures++;
                                        dtimeleft--;
                                        con.tracker(usedev, dtimeleft, lognum, missdev, missdays, rotationdev, failures);
                                        break;
                                    case 3:
                                        ErrorMsg = Environment.NewLine + "Device " + BKDevice + " is not online. It has " + (dtimeleft - 1) + " sequences left.";
                                        Console.WriteLine("Device " + BKDevice + " is not online. It has " + (dtimeleft - 1) + " sequences left.");
                                        ErrorMsg = Environment.NewLine + @"This backup will occur on " + BKD[usedev] + @" until " + BKD[rotationdev] + " is returned or its sequence expires";
                                        Console.WriteLine(@"Backups will continue to occur on " + BKD[usedev] + @" until " + BKD[rotationdev] + " is returned or its sequence expires");
                                        missdev = rotationdev;
                                        missdays++;
                                        break;
                                }


                            }





                            #endregion


                            DriveInfo BKdrive = new DriveInfo(bkdmap);

                            aspace = BKdrive.AvailableFreeSpace;
                            Console.WriteLine(@"Available Free Space on Target= " + con.ConvertBytes(aspace));
                            File.AppendAllText(sumpath, Environment.NewLine);
                            File.AppendAllText(logfile, Environment.NewLine + @"Device Mapping:" + bdrivename);
                            Console.WriteLine(@"Device Mapping:" + bdrivename);
                            File.AppendAllText(logfile, @"  Backup Device:" + BKD[usedev]);
                            File.AppendAllText(logfile, Environment.NewLine + @"Available Space on Backup Device:" + con.ConvertBytes(aspace));
                            File.AppendAllText(sumpath, Environment.NewLine + @"Device Mapping:" + bdrivename);
                            File.AppendAllText(sumpath, @"  Backup Device:" + BKD[usedev]);
                            File.AppendAllText(sumpath, Environment.NewLine + @"Available Space on Backup Device:" + con.ConvertBytes(aspace));



                            #region Run Backup
                            List<string> missingpaths = new List<string>();
                            StartTime=DateTime.Now;
                            File.AppendAllText(sumpath, Environment.NewLine + @"Backup Started " + StartTime.ToString());
                            Console.WriteLine(@"Checking Source for directories...");
                            File.AppendAllText(sumpath, Environment.NewLine + @"Source for directories:");
                            File.AppendAllText(logfile, Environment.NewLine + @"Source directories");
                            foreach (string loop in BPaths)//get size total of files to be backed up
                            {
                                if (!(loop == null))
                                {
                                    if (Directory.Exists(loop))
                                    {
                                        DirectoryInfo dir = new DirectoryInfo(loop);
                                        long tspace = DirSize(dir, BKType);
                                        rspace = rspace + tspace;
                                        Console.WriteLine(loop + "--" + con.ConvertBytes(tspace));
                                        File.AppendAllText(sumpath, Environment.NewLine + loop + "--" + con.ConvertBytes(tspace));
                                        File.AppendAllText(logfile, Environment.NewLine + loop + "--" + con.ConvertBytes(tspace));
                                    }
                                    else
                                    {
                                        Console.WriteLine(loop + @" not found and will be skipped");
                                        File.AppendAllText(sumpath, Environment.NewLine + loop + @" not found and skipped");
                                        File.AppendAllText(logfile, Environment.NewLine + loop + @" not found and will be skipped");
                                        missingpaths.Add(loop);
                                    }
                                }
                               
                            }
                            foreach (string mpath in missingpaths)
                            {
                                if (BPaths.Contains(mpath))
                                {
                                    BPaths.Remove(mpath);
                                }
                            }
                            if (aspace - (rspace + sysstatesize) < 100) outaspace();
                            if (!(Directory.Exists(bdrivename + "DRCBKUPS")))
                            {
                                Directory.CreateDirectory(bdrivename + @"DRCBKUPS\" + DateTime.Now.DayOfWeek.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + "BK1");
                                targetdir = bdrivename + @"DRCBKUPS\" + DateTime.Now.DayOfWeek.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + "BK1";
                            }
                            else
                            {
                                int x = 1;
                                int bkdircount = 0;
                                string oldestbk = "";
                                DirectoryInfo bkdev = new DirectoryInfo(bdrivename + @"DRCBKUPS");
                                foreach (DirectoryInfo loop in bkdev.GetDirectories())
                                {
                                    if (loop.FullName.EndsWith(x.ToString())) bkdircount++;
                                    if (loop.FullName.EndsWith("1")) oldestbk = loop.FullName;
                                    x++;
                                }
                                if (bkdircount < loglife)
                                {
                                    Directory.CreateDirectory(bdrivename + @"DRCBKUPS\" + DateTime.Now.DayOfWeek.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + "BK" + (bkdircount + 1).ToString());
                                    targetdir = bdrivename + @"DRCBKUPS\" + DateTime.Now.DayOfWeek.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + "BK" + (bkdircount + 1).ToString();
                                }
                                else
                                {
                                    Directory.Delete(oldestbk, true);
                                    Directory.CreateDirectory(bdrivename + @"DRCBKUPS\" + DateTime.Now.DayOfWeek.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + "BK1");
                                    targetdir = bdrivename + @"DRCBKUPS\" + DateTime.Now.DayOfWeek.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + "BK1";
                                }
                            }


                            if (BackupSysState) //Backup System State First if true
                            {
                                string SysBKstartMSG = "SystemState Backup Started to " + targetdir + @"\ServSysState.bkf";
                                string SysStateARG = "backup systemstate /J \"ServerSystemState\" /V:yes /R:no /RS:no /HC:off /L:f /M normal /F \"" + targetdir + "\\ServSysState.bkf\"";
                                Console.WriteLine(@"Starting Backup to " + bdrivename + @"(" + BKD[usedev] + @")");
                                if (BKType == "D")
                                {
                                    SysBKstartMSG = "SystemState Differential Backup Started to " + targetdir + @"\ServSysState.bkf";
                                    SysStateARG = "backup systemstate /J \"ServerSystemState\" /V:yes /R:no /RS:no /HC:off /L:f /M differential /F \"" + targetdir + "\\ServSysState.bkf\"";
                                }
                                Console.WriteLine(SysBKstartMSG);
                                File.AppendAllText(sumpath, Environment.NewLine + SysBKstartMSG);

                                try
                                {
                                    System.Diagnostics.Process BSysState = new System.Diagnostics.Process();
                                    BSysState.StartInfo.FileName = "ntbackup.exe";
                                    //store systems state backup in first source path directory so it is accounted for when size requirments are checked
                                    BSysState.StartInfo.Arguments = SysStateARG;
                                    BSysState.Start();
                                    BSysState.WaitForExit();
                                    File.AppendAllText(sumpath, Environment.NewLine + @"System State Backup Complete");
                                    Console.WriteLine(@"System State Backup Complete");
                                }
                                catch (Exception ess)
                                {
                                    File.AppendAllText(sumpath, Environment.NewLine + @"System State Backup Failed to Complete ERROR: " + ess.Message);
                                    Console.WriteLine(@"System State Backup Failed to Complete ERROR: " + ess.Message);
                                }
                            }

                            Console.WriteLine("Data Backup Started");


                            copydir();

                            ErrorMsg = Environment.NewLine + @"Total backups in this sequence= " + BKDAYS[rotationdev];
                            logerrors(ErrorMsg);
                            Console.Write(ErrorMsg);
                            ErrorMsg = Environment.NewLine + @"Successfull backups for this sequence= " + (Convert.ToInt32(BKDAYS[rotationdev]) - failures - dtimeleft);//Extra minus one is because dtimeleft is not actually decremented until the copy proccess is over.
                            logerrors(ErrorMsg);
                            Console.Write(ErrorMsg);
                            ErrorMsg = Environment.NewLine + @"Backup failures for this sequence= " + failures;
                            logerrors(ErrorMsg);
                            Console.Write(ErrorMsg);
                            ErrorMsg = Environment.NewLine + @"Backups to other devices for this sequence= " + missdays;
                            logerrors(ErrorMsg);
                            Console.Write(ErrorMsg);
                            int nextdevice = 0;//set next device to first device
                            if (rotationdev + 2 > BKDCount) nextdevice = 0; //plus 2 because the counts are zero based.
                            else nextdevice = rotationdev + 1;
                            ErrorMsg = Environment.NewLine + @"The Next device in the rotation is " + BKD[nextdevice] + ".";
                            File.AppendAllText(sumpath, ErrorMsg);
                            Console.Write(ErrorMsg);
                            ErrorMsg = Environment.NewLine + @"Detailed log files are located in " + logpath + "on the program's host computer.";
                            File.AppendAllText(sumpath, ErrorMsg);
                            Console.Write(ErrorMsg);
                            ErrorMsg = Environment.NewLine + "The Log for this backup= " + logfile + Environment.NewLine;
                            File.AppendAllText(sumpath, ErrorMsg);
                            Console.Write(ErrorMsg);
                            ErrorMsg = Environment.NewLine + @"The Next device in the rotation is " + BKD[nextdevice] + ".";
                            File.AppendAllText(logfile,ErrorMsg);
                            Console.Write(ErrorMsg);
                            if (netdrive != "")//Remove Drive mapping if any
                            {
                                System.Diagnostics.Process mapnet = new System.Diagnostics.Process();
                                mapnet.StartInfo.FileName = "net.exe";
                                mapnet.StartInfo.Arguments = @" use " + netdrive + @": /DELETE";
                                mapnet.Start();
                                mapnet.WaitForExit();
                            }
                            EndTime=DateTime.Now;
                            File.AppendAllText(sumpath, Environment.NewLine + @"Backup Completed " + EndTime.ToString());
                            File.AppendAllText(sumpath,Environment.NewLine+@"Total Backup Time= "+(EndTime-StartTime).ToString().Substring(0,8));
                            Console.WriteLine("Backup Complete");
                            
                            #endregion
                        }
                        // email();
                 //   con.notify(File.ReadAllText(sumpath), emailhost, emailrecipients, logfile,emailreturnaddr);
                    con.notify2(File.ReadAllText(sumpath), emailhost,emailport ,emailrecipients, logfile, emailreturnaddr);
                   
                }
            }
        }
        //-----------------------------------------------------------END of Main--------------------------------------------------------------------
        public static long DirSize(DirectoryInfo d, string bktype)
        {
            string baddir = "";
            string badfile = "";
            try
            {
                long Size = 0;
                // Add file sizes.
                FileInfo[] fis = d.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    if (bktype == "D") //Differential backup size
                    {
                        if ((File.GetAttributes(fi.FullName) & FileAttributes.Archive) == FileAttributes.Archive)
                        {
                            Size += fi.Length;
                            badfile = fi.FullName;
                        }

                    }
                    else//Full Backup size-incremental not an option
                    {
                        Size += fi.Length;
                        badfile = fi.FullName;
                    }
                }
                // Add subdirectory sizes.
                DirectoryInfo[] dis = d.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    Size += DirSize(di, bktype);
                    baddir = di.FullName;
                }
                return (Size);
            }
            catch
            {
                File.AppendAllText(logfile, Environment.NewLine + @"Invalid file/directory-Directory:" + baddir + @"  File:" + badfile);
                return (0);
            }
        }
        //##########################################################This calls itself to go through all directories##################################################################
        private static void CopyDirs(string sourcePath, string destPath, string bktype)
        {


            string baddir = sourcePath;
            string badfile = destPath;
            Console.ForegroundColor = ConsoleColor.Green;
            Directory.CreateDirectory(destPath);

            //File.AppendAllText(logfile,Environment.NewLine+baddir + "----"+badfile);
            foreach (string subDirectoryPath in Directory.GetDirectories(sourcePath))
            {
                dircount++;
                try
                {
                    baddir = con.Name(subDirectoryPath);
                    // File.AppendAllText(logfile,Environment.NewLine+baddir);
                    CopyDirs(subDirectoryPath, destPath + @"\" + con.Name(subDirectoryPath), bktype);
                    File.SetAttributes(destPath + @"\" + con.Name(subDirectoryPath), FileAttributes.Normal);
                }

                catch
                {
                    File.AppendAllText(logfile, Environment.NewLine + Environment.NewLine + @"Invalid directory-:" + baddir + Environment.NewLine);
                }
            }


            foreach (string filePath in Directory.GetFiles(sourcePath))
            {
                filecount++;

                if (bktype == "D")
                {
                    if ((File.GetAttributes(filePath) & FileAttributes.Archive) == FileAttributes.Archive)
                    {
                        try
                        {
                            badfile = con.Name(filePath);
                            File.Copy(filePath, destPath + @"\" + con.Name(filePath), true);
                            File.SetAttributes(destPath + @"\" + con.Name(filePath), FileAttributes.Normal);
                            con.archiveoff(filePath);
                            Console.WriteLine("Copied: " + destPath + @"\" + con.Name(filePath));
                            File.AppendAllText(logfile, Environment.NewLine + "Copied: " + destPath + @"\" + con.Name(filePath) + "\n");
                            bfiles++;
                        }
                        catch
                        {
                            File.AppendAllText(logfile, Environment.NewLine + @"Invalid File-:" + baddir);
                        }
                    }
                }
                else
                {
                    try
                    {
                        badfile = con.Name(filePath);
                        File.Copy(filePath, destPath + @"\" + con.Name(filePath), true);
                        File.SetAttributes(destPath + @"\" + con.Name(filePath), FileAttributes.Normal);
                        con.archiveoff(filePath);
                        Console.WriteLine("Copied: " + destPath + @"\" + con.Name(filePath));
                        File.AppendAllText(logfile, Environment.NewLine + "Copied: " + destPath + @"\" + con.Name(filePath) + "\n");
                        bfiles++;
                    }
                    catch
                    {
                        File.AppendAllText(logfile, Environment.NewLine + @"Invalid File-:" + baddir);
                    }
                }
            }
        }
    
        //#####################################################Remove drive from path###############################################################################################
       
        //All this to remove the archive bit!
        
        public static void logerrors(string err)
        {
            File.AppendAllText(sumpath, err + Environment.NewLine);
            File.AppendAllText(logfile, err + Environment.NewLine);
            File.AppendAllText(@"C:\DRCTECH\error.log", DateTime.Now.ToString() + err);
        }

        //-----------------------------------------Update Marker Routine----------------------------------------------------------------------------
        
        public static void copydir()
        {
            //####################################################Copy Directories and Files###################################################################################
            if (ErrorCode == 0)
            {

                foreach (string bdir in BPaths)
                {
                    string pathname = con.PathName(bdir);
                    if (!(bdir == null))
                    {
                        CopyDirs(bdir, targetdir +"\\"+pathname+ con.Name(bdir), BKType);
                    }
                }
                dtimeleft--;

                //Record program traking information here
                con.tracker(usedev, dtimeleft, lognum, missdev, missdays, rotationdev, failures);



                //change if the next device in rotation is not the first one.
                File.AppendAllText(logfile, dircount.ToString() + " Directories Checked");
                File.AppendAllText(logfile, Environment.NewLine + filecount.ToString() + " Files Checked");
                File.AppendAllText(logfile, Environment.NewLine + bfiles.ToString() + " Files Backed Up");
                File.AppendAllText(logfile, Environment.NewLine + @"Sequences in the rotation remaining for device " + BKD[rotationdev] + @": " + dtimeleft.ToString());

                File.AppendAllText(sumpath, Environment.NewLine + dircount.ToString() + " Directories Checked");
                File.AppendAllText(sumpath, Environment.NewLine + filecount.ToString() + " Files Checked");
                File.AppendAllText(sumpath, Environment.NewLine + bfiles.ToString() + " Files Backed Up");
                File.AppendAllText(sumpath, Environment.NewLine + @"Sequences in the rotation remaining for device " + BKD[rotationdev] + @": " + dtimeleft.ToString() + Environment.NewLine);


                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


            }
        }
        
       
        public static void outaspace()
        {
            Console.WriteLine("Making more space");
            ErrorMsg = Environment.NewLine + @"Required space for backup exceeds available space on " + BKD[usedev];
            Console.WriteLine(@"Required space for backup exceeds available space on " + BKD[usedev]);
            logerrors(ErrorMsg);
            ErrorMsg = Environment.NewLine + @"Older backups will be removed until the available space is aquired!";
            Console.WriteLine(@"Older backups will be removed until the available space is aquired!");
            logerrors(ErrorMsg);
            DriveInfo BKdrive = new DriveInfo(bkdmap);
            DirectoryInfo mkspace = new DirectoryInfo(bdrivename + @"DRCBKUPS");
            DateTime oldestbackup = DateTime.Now;
            string deleteme = "";
            string deleted = "";
            ErrorMsg = Environment.NewLine + @"The following backups have been removed:" + Environment.NewLine;
            logerrors(ErrorMsg);
            
            while (aspace - rspace < 20)
            {
                oldestbackup = DateTime.Now;
                if (mkspace.GetDirectories().ToList().Count < 1)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("All previous backups have been removed and their still is not enough space");
                    Console.WriteLine("Capacity of device is no longer large enough or files other than backups are using the space");
                    Console.WriteLine("Backup can not be completed");
                    ErrorMsg = deleted + "All previous backups have been removed and their still is not enough space.";
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                }
                foreach (DirectoryInfo deldir in mkspace.GetDirectories())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(@"Checking Directory " + deldir.FullName);
                    if (deldir.CreationTime < oldestbackup)
                    {
                        deleteme = deldir.FullName;
                        deleted = deldir.Name;
                        oldestbackup = deldir.CreationTime;
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                }
                if(Directory.Exists(deleteme))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(@"Deleting Directory:"+deleteme);
                Directory.Delete(deleteme, true);
                ErrorMsg = deleted + Environment.NewLine;
                logerrors(ErrorMsg);
                Console.ForegroundColor = ConsoleColor.White;
                }
                aspace = BKdrive.AvailableFreeSpace;

            }
       }
   }
    
}
