using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Mail;
namespace DRCBackup
{
    class Configurator
    {
        
        public List<string> inputsection(string section)
        {
            List<string> csection = readsection(section);
            string itemin = "item";
            int seccount = csection.Count;
            int startcount = seccount;
            int loopcount = 0;
            string instructions = "";
            string warning = "";
            string item = "";
            bool oktoadditem = false;
            bool promptoptions = false;
            
            switch (section)
            {
                case "<BACKUP DEVICES>":
                    instructions = "Enter Device Paths or volume labels, Enter 0 when finished";
                    warning = "No Target devices entered, backup will not occur without at least one target.";
                    item = "Target";
                    break;
                case "<PATHS>":
                    instructions = "Enter backup sources. Enter 0 when finished.";
                    warning = "No sources saved, nothing will be backed up unless a source is added.";
                    item = "Source";
                    break;
                case "<EMAIL>":
                    promptoptions = true;
                    instructions = "Change or enter blank to keep current setting.";
                    break;
                case "<EMAILRECIPIENTS>":
                    instructions = "Enter Email addresses, Enter 0 when finished";
                    warning = "No email addresses entered, no alerts will be sent.";
                    item = "Email Address";
                    break;
                case "<DEVDAYS>":
                    promptoptions = true;
                    instructions = "Enter number of days to use each device before rotation.";
                    break;
                case "<JOBS>":
                    promptoptions = true;
                    instructions = "Change or enter blank to keep current setting";
                    break;
                case "<DAILY>":
                    promptoptions = true;
                    instructions = "Change or enter blank to keep current setting. Valid entries are D or F";
                    break;
                default:
                    break;
            }
            Console.WriteLine(instructions);
            if (promptoptions)
            {
                List<string> options=new List<string>();
                List<string> seloptions = new List<string>();
                List<string> validationtype = new List<string>();
                switch (section)
                {
                    case "<EMAIL>":
                        options.Add("Sending Host"); options.Add("Port Number"); options.Add("Return Address");
                        validationtype.Add("HOST"); validationtype.Add("+INT"); validationtype.Add("EMAILADR");
                        break;
                    case "<DEVDAYS>":
                        if (csection.Count < 1) csection.Add("7");//No Days, add at least one
                        foreach (string tar in readsection("<BACKUP DEVICES>"))
                        {
                            options.Add(tar);
                            validationtype.Add("+INT");
                            if (options.Count > csection.Count) csection.Add("7");//All devices must have days, if it does not a default is added here
                        }

                        break;
                    case "<JOBS>":
                        options.Add(@"Log Folder, Make sure to end with \"); options.Add("Days To Keep Log before overwriting");
                        options.Add(@"Backup System State? true or false");
                        validationtype.Add("YN");
                        validationtype.Add("PATH"); validationtype.Add("+INT");
                        break;
                    case "<DAILY>":
                        options.Add("Monday"); options.Add("Tuesday"); options.Add("Wednesday"); options.Add("Thursday"); options.Add("Friday"); options.Add("Saturday"); options.Add("Sunday");
                        for (int i = 0; i < options.Count; i++)
                        {
                            validationtype.Add("BKTYPE"); 
                        }
                        
                        break;
                    default:
                        break;
                }
                loopcount = 0;
                foreach (string opt in csection)
                {
                    Console.WriteLine(options[loopcount] +"("+opt+"):");
                    itemin = Console.ReadLine();
                    itemin = itemin.ToUpper();
                    if (itemin == "")
                    {
                        Console.WriteLine(opt);
                        seloptions.Add(opt);
                    }
                    else
                    {
                        switch (validationtype[loopcount])
                        {
                            case "HOST":
                                seloptions.Add(itemin);
                                break;
                            case "+INT":
                                seloptions.Add(itemin);
                                break;
                            case "EMAILADR":
                                seloptions.Add(itemin);
                                break;
                            case "PATH":
                                seloptions.Add(itemin);
                                break;
                            case "BKTYPE":
                                seloptions.Add(itemin);
                                break;
                            case "YN":
                                seloptions.Add(itemin);
                                break;

                            default:
                                break;
                        }
                    }
                    
                    loopcount++;
                }
                csection = seloptions;
            }
            else
            {
                while (itemin != "0")
                {
                    if (loopcount < startcount)
                    {
                        Console.WriteLine("Change, Press Enter to Keep or D to delete.");
                        Console.WriteLine(loopcount + 1 + @": " + csection[loopcount]);
                    }
                    else
                    {
                        Console.WriteLine(@"Enter new " + item + @" or Enter 0 to Exit");
                        Console.WriteLine(loopcount + 1 + @": ");
                    }
                    itemin = Console.ReadLine();
                    itemin = itemin.ToUpper();
                    
                        switch (itemin)
                        {
                            case "":
                                try
                                {
                                    itemin = csection[loopcount];
                                    Console.WriteLine(csection[loopcount] + @" retained.");
                                }
                                catch
                                {
                                    Console.WriteLine(@"Blank Entry Invalid when no previous entry exists");
                                    loopcount--;
                                }
                                break;
                            case "D":
                                csection.RemoveAt(loopcount);
                                loopcount--;
                                startcount--;
                                break;
                            case "0":
                                //Skip default
                                break;
                            default:

                                switch (section)
                                {
                                    case "<BACKUP DEVICES>":
                                        if (itemin.Contains(@"\"))//path
                                        {
                                            if (itemin.Substring(itemin.Length) == @"\") itemin = itemin.Substring(0,itemin.Length-1);//insures it maps properly
                                            if (valpathinput(itemin, "") != "")
                                            {
                                                oktoadditem = true;
                                            }
                                        }
                                        else //volume label
                                        {
                                            oktoadditem = true;
                                        }

                                        break;
                                    case "<PATHS>":
                                        if (valpathinput(itemin, "") != "")
                                        {
                                            oktoadditem = true;
                                        }
                                    
                                        break;
                                    case "<EMAILRECIPIENTS>":
                                        if (itemin.Contains(@"@") && itemin.Contains(@".")) oktoadditem = true;
                                        break;
                                    default:
                                        break;
                                }
                                if (oktoadditem)
                                {
                                    csection.Add(itemin); 
                                    Console.WriteLine(csection[seccount] + @" added.");
                                    seccount++;
                                }
                                else
                                {
                                    Console.WriteLine("To enter without validation, reenter now. Note that invalid items entered without validation could cause application failure");
                                    Console.WriteLine("If Not, press enter to try again");
                                    if (Console.ReadLine() == itemin)
                                    {
                                        csection.Add(itemin);
                                        Console.WriteLine(@"Unverified Item " + csection[seccount] + @" added.");
                                        seccount++;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Adding of Unverifiable Item Canceled");
                                       // Console.WriteLine("Enter Items, Enter blank when finished");
                                        loopcount--;
                                        itemin = "skipit";
                                    }

                                }
                                break;
                        }


                    

                    loopcount++;
                }
            }
            if (csection.Count < 1)
            {
                Console.WriteLine(warning);
                inputsection(section);
            }
            else
            {
                writesection(csection,section);
            }

            return csection;
        }
        public void writedevices(List<string> devices)
        {
            
            List<string> currentfile = LoadConfig();
            List<string> newfile = new List<string>();
            int sectionstartIDX = 0;
            int sectionendIDX;
            foreach (string item in currentfile)//copy 1st half
            {
                sectionstartIDX++;
                if (item != "<BACKUP DEVICES>")
                {
                    newfile.Add(item);
                }
                else break;

            }
            sectionendIDX=sectionstartIDX;
            for (int i = sectionstartIDX; i < currentfile.Count; i++)
            {
                if (currentfile[i]!="<END>")
	{
		 sectionendIDX++;
	}
                else break;
            }
            sectionendIDX++;
            newfile.Add("<BACKUP DEVICES>");
            foreach (string dev in devices)
            {
                newfile.Add(dev);
            }
            newfile.Add("<END>");
            for (int I2 = sectionendIDX; I2 < currentfile.Count; I2++)
			{
                newfile.Add(currentfile[I2]);
			}
            
          StreamWriter wdevice = new StreamWriter(@"c:\DRCTECH\DrcBackupSys.ini");

          foreach (string line in newfile)
          {
              wdevice.WriteLine(line);
          }
          wdevice.Close();
          wdevice.Dispose();
            
        }
        public List<string> LoadConfig()
        {
            
            List<string> conf = new List<string>();
            StreamReader loadconfig = new StreamReader(@"c:\DRCTECH\DrcBackupSys.ini");
            while (!(loadconfig.EndOfStream))
            {
                conf.Add(loadconfig.ReadLine());
            }
            loadconfig.Close();
            loadconfig.Dispose();
            
            return conf;
        }
        public List<string> readdevdays()
        {
            StreamReader getdays = new StreamReader(@"c:\DRCTECH\DrcBackupSys.ini");
            List<string> days = new List<string>();
            string line = "";
            while (!(line=="<DEVDAYS>"))
            {
                line = getdays.ReadLine();
                
            }
            while (!(line == "<END>"))
            {
                line = getdays.ReadLine();
                days.Add(line);
            }
            days.Remove("<END>");
            getdays.Close();
            getdays.Dispose();
            return days;
        }
        public List<string> inputdevdays(List<string> devpaths)
        {
            List<string> currentdays = readdevdays();
            List<string> outdays=new List<string>();
            string days = "7";
            string choice = "";
            for (int i = 0; i < devpaths.Count; i++)
            {
                try
                {
                    days=currentdays[i];
                }
                catch
                {
                    days = "7";
                }
                Console.WriteLine("Enter number of consecutive days to use device in rotation");
                Console.WriteLine("or press enter to keep the current or default setting.");
                choice = valintinput(devpaths[i]+@" ["+days+@"]");
                if (!(choice == ""))
                {
                    days = choice;
                }
                outdays.Add(days);
            }
            return outdays;
        }
        public void writesection(List<string> seclines,string secname)
        {
            List<string> currentfile = LoadConfig();
            List<string> newfile = new List<string>();
            int sectionstartIDX = 0;
            int sectionendIDX;
            foreach (string item in currentfile)//copy 1st half
            {
                sectionstartIDX++;
                if (item != secname)
                {
                    newfile.Add(item);
                }
                else break;

            }
            sectionendIDX = sectionstartIDX;
            for (int i = sectionstartIDX; i < currentfile.Count; i++)
            {
                if (currentfile[i] != "<END>")
                {
                    sectionendIDX++;
                }
                else break;
            }
            sectionendIDX++;
            newfile.Add(secname);
            foreach (string dev in seclines)
            {
                newfile.Add(dev);
            }
            newfile.Add("<END>");
            for (int I2 = sectionendIDX; I2 < currentfile.Count; I2++)
            {
                newfile.Add(currentfile[I2]);
            }

            StreamWriter wdevice = new StreamWriter(@"c:\DRCTECH\DrcBackupSys.ini");

            foreach (string line in newfile)
            {
                wdevice.WriteLine(line);
            }
            wdevice.Close();
            wdevice.Dispose();
        }
        public string valintinput(string message)
        {
            
            Console.WriteLine(message);
            string sel = Console.ReadLine();
            if (sel != "")
            {
                try { Convert.ToInt32(sel); }
                catch
                {
                    Console.WriteLine(@"Entry must be blank or a valid integer. Use 0 for infinity.");
                    sel = valintinput(message);
                }
            }
                    return sel;
        }
        public List<string> readsection(string secname)
        {
            StreamReader getlines = new StreamReader(@"c:\DRCTECH\DrcBackupSys.ini");
            List<string> seclines = new List<string>();
            string line = "";
            while (!(line == secname))
            {
                line = getlines.ReadLine();

            }
            while (!(line == "<END>"))
            {
                line = getlines.ReadLine();
                seclines.Add(line);
            }
            seclines.Remove("<END>");
            getlines.Close();
            getlines.Dispose();
            return seclines;
        }
        public List<string> getonlinevolumes()
        {
           DriveInfo[] alldrives = DriveInfo.GetDrives();
            List<string> onvols=new List<string>();
            foreach (DriveInfo drive in alldrives)
            {
                if (drive.IsReady)
                {
                    onvols.Add(drive.VolumeLabel);
                }
            }
            return onvols;
        }
        public int getdrive(List<string> allrotdevs, int rotdev)
        {
            Console.WriteLine("getdrive(allrotdevs,"+rotdev+")");
            int usedrive = -1;
            List<string> OnlineVolumes = getonlinevolumes();
            int devidx = rotdev;
            int endloopidx = rotdev;
           // if (endloopidx == -1) endloopidx = allrotdevs.Count;//rotdev is first device in rotation
            Console.WriteLine("while(" + devidx + @"!=" + endloopidx + ")");
            do //start checking for devices at the rotation number and check through all other devices to the last and then check from beginning to the rotation number. Exit before completing if one is found online.
            {
                Console.WriteLine(@"allrotdevs[" + devidx + @"]= " + allrotdevs[devidx]);
                if (allrotdevs[devidx].Contains(@"\"))
                {
                    if (Directory.Exists(allrotdevs[devidx])) usedrive = devidx;

                }
                else
                {
                    Console.WriteLine("foreach(string item in OnlineVolumes)");
                    foreach (string item in OnlineVolumes)
                    {
                        Console.WriteLine("item=" + item);
                        if (allrotdevs[devidx] == item)
                        {
                            usedrive = devidx;
                            break;
                        }
                    }

                }
                if (usedrive != -1) break;
                devidx++;
                if (devidx == allrotdevs.Count)
                {
                    devidx = 0; //reached last rotation device start at begining
                    endloopidx = rotdev;//only search from begining to the search device
                }


            } while (devidx != endloopidx);

   
            Console.WriteLine(@"Returning usedrive from getdrive " + usedrive);
            return usedrive;
            
        }
        public void resetmark()
        {
            
            List<string> days = readsection("<DEVDAYS>");
            
            StreamWriter wrmark = new StreamWriter(@"c:\DRCTECH\DrcBackupSys.mrk");
            
            wrmark.WriteLine(-1);
            wrmark.WriteLine(0);
            wrmark.WriteLine(days[0]);
            wrmark.WriteLine(-1);
            wrmark.WriteLine(-1);
            wrmark.WriteLine(-1);
            wrmark.WriteLine(0);
            
            wrmark.Close();
            wrmark.Dispose();
            return;
        }
        public void runconfig()
        {
            Console.Clear();
            Console.WriteLine(@"DRCBACUP CONFIGURATION UTILITY");
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine(@"1-Reset Mark File  2-Targets  3-Sources  4-Types  5-Alerts  6-Logs  0-Exit");
            ConsoleKeyInfo choice = Console.ReadKey();
            switch (choice.KeyChar.ToString())
            {
                case "1":
                    resetmark();
                    runconfig();
                    break;
                case "2":
                    Console.Clear();
                    List<string> Devconfs=inputsection("<BACKUP DEVICES>");
                    List<string> DevDaysconfs = inputsection("<DEVDAYS>");
                    runconfig();
                    break;
                case "3":
                    Console.Clear();
                    List<string> Pathconfs=inputsection("<PATHS>");
                    runconfig();
                    break;
                case "4":
                    List<string> Typeconfs = inputsection("<DAILY>");
                    runconfig();
                    break;
                case "5":
                    List<string> Alertconfs=inputsection("<EMAIL>");
                    List<string> Recipconfs = inputsection("<EMAILRECIPIENTS>");
                    runconfig();
                    break;
                case "6":
                    List<string> Logconfs = inputsection("<JOBS>");
                    runconfig();
                    break;
                case "0":
                    return;
                    break;
                default:
                    return;
                    break;
            }
        }
        public List<string> inputpaths(List<string> existingpaths)
        {
            List<string> newpaths = existingpaths;
            string entry = "start";
            Console.WriteLine("Enter paths to backup. Enter 0 when completed. D to delete existing path");
            int IDX = 0;
            while (entry != "0")
            {

                try
                {
                    Console.WriteLine(IDX + @": " + existingpaths[IDX] + @" (Change or Enter blank to keep or D to delete)");
                    entry = Console.ReadLine();
                    if (entry.ToUpper() == "D") { newpaths.RemoveAt(IDX); }
                    else
                    {
                        if (entry == "") { entry = existingpaths[IDX]; }
                    }

                }
                catch
                {
                    Console.WriteLine(IDX + @": Enter new Path");
                    entry = Console.ReadLine();
                }
                if (entry == "0") break;
                if (entry.ToUpper() != "D")
                {
                    do
                    {
                        entry = valpathinput(entry, "");
                        if (entry == "") entry = valpathinput(Console.ReadLine(), "");
                    } while (entry == "");
                    newpaths.Add(entry);
                    Console.WriteLine(entry + @" added to backup.");
                    IDX++;
                }
                else { Console.WriteLine(existingpaths[IDX] + @" removed from backup"); }
            }


            return newpaths;
        }
        public List<string> inputjob(List<string> jbs)
        {
            List<string> njbs = new List<string>();
            string ldir="";
            int loglife=-1;
            string BkType = "";
            Console.WriteLine("No Job Configured, configure job");
            Console.WriteLine("Enter Directory in which to store log files");
            Console.WriteLine(@"Enter blank to keep Current Directory: "+jbs[1]);
            while (ldir=="")
            {
                ldir = valpathinput(Console.ReadLine(),jbs[1]);//"" returned if invalid
            }
            jbs[1]=ldir;
            Console.WriteLine();
            Console.WriteLine("Enter number of logs to keep before overwriting");
            while (loglife == -1)
            {
                loglife = valposintinput(Console.ReadLine(), jbs[3]);//-1 returned if invalid
            }
            jbs[3] = loglife.ToString();
            int IDX = 5;
            while (IDX<19)
            {
                Console.WriteLine(@"Enter Backup Type for " + jbs[IDX] + @" (F=Full,D=Differential,N=None)");
                while (BkType=="")
                {
                    BkType = valBKTypeinput(Console.ReadLine(), jbs[IDX + 1]);
                }
                jbs[IDX + 1] = BkType;
                IDX = IDX + 2;
                BkType = "";
            }
            return jbs;
        }
        public string valpathinput(string chkpath,string pdefault)
        {
            if (chkpath == "") chkpath = pdefault;
            if (!(Directory.Exists(chkpath)))
            {
                Console.WriteLine("Path not found. Enter a valid path.");
                chkpath = "";
                
            }
            return chkpath;
        }
        public int valposintinput(string schkint, string sintdefault)
        {
            int chkint = 0;
            if (schkint == "") schkint = sintdefault;
            try { 
                chkint = Convert.ToInt32(schkint);
                if (chkint < 0)
                {
                    Console.WriteLine("Must be a positive integer. Enter a positive integer");
                    chkint = -1;
                }
            }
            catch
            {
                Console.WriteLine("Not a valid integer. Enter a valid integer.");
                chkint = -1;
            }
            return chkint;
        }
        public string valBKTypeinput(string chkbkt, string btdefault)
        {
            if (chkbkt == "") chkbkt = btdefault;
            chkbkt = chkbkt.ToUpper();
            if (!(chkbkt == "F" || chkbkt == "D" || chkbkt == "N"))
            {
                Console.WriteLine(@"Enter Valid Backup Type (F=Full,D=Differential,N=None)");
                chkbkt = "";
            }
            return chkbkt;
        }
        public string findnetdrive()
        {
            List<int> drives = new List<int>();
            string cnetd = "";
            int netdint = 66;
            foreach (DriveInfo nd in DriveInfo.GetDrives())
            {
                drives.Add(Convert.ToChar(nd.Name.Substring(0,1)));
            }
            
            while (cnetd=="")
            {
                cnetd = Convert.ToChar(netdint).ToString();
                foreach (int netdchar in drives)
                {
                    
                    if (netdchar==netdint)
                    {
                        cnetd = "";
                    }
                }
                netdint++;
            }
            return cnetd;
        }
        public void notify(string msg, string MailHost, List<string> alertRCPT, string logfile, string senderadr)
        {
            File.AppendAllText(logfile, Environment.NewLine + @"Notification Started");
            Console.WriteLine(@"Notification Started");
            
            SmtpClient myhost = new System.Net.Mail.SmtpClient(MailHost);
            MailMessage mess = new MailMessage();
            mess.Subject = @"DRCTECH Backup Summary";
            mess.From = new MailAddress(senderadr);
            mess.Body = msg;
        //    mess.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess;
          //  mess.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
            foreach (string recpt in alertRCPT)
            {
                mess.To.Add(new MailAddress(recpt));
                Console.WriteLine(@"Adding alert for " + recpt);
            }
            try
            {
                myhost.Send(mess);

                File.AppendAllText(logfile, Environment.NewLine + @"Notification Complete");
                Console.WriteLine(@"Notification Complete");
            }
            catch (Exception ae)
            {
                File.AppendAllText(logfile, Environment.NewLine + @"Notification Failed: "+ae);
                Console.WriteLine(@"Notification Failed: " + ae);
                
            }
            
        }
        public void notify2(string msg, string MailHost, int eport, List<string> alertRCPT, string logfile, string senderadr)
        {
            File.AppendAllText(logfile, Environment.NewLine + @"Notification Started");
            try
            {
                TcpClient DRCSmtp = new TcpClient(MailHost, eport);
                List<string> recev = new List<string>();
                string sendrtxt;
                string recvtxt;
                string CRLF = "\r\n";
                byte[] sdata;
                NetworkStream estrem = DRCSmtp.GetStream();
                StreamReader rstrem = new StreamReader(DRCSmtp.GetStream());
                recvtxt = rstrem.ReadLine();
                File.AppendAllText(logfile, Environment.NewLine + recvtxt);
                Console.WriteLine(recvtxt);
                sendrtxt = "HELO " + MailHost + CRLF;
                Console.Write(sendrtxt);
                sdata = System.Text.Encoding.ASCII.GetBytes(sendrtxt.ToCharArray());
                estrem.Write(sdata, 0, sdata.Length);

                recvtxt = rstrem.ReadLine();
                Console.WriteLine(recvtxt);
                File.AppendAllText(logfile, Environment.NewLine + recvtxt);


                foreach (string a in alertRCPT)
                {
                    try
                    {
                       
                        
                        sendrtxt = "MAIL FROM: " + "<" + senderadr + ">" + CRLF;
                        Console.Write(sendrtxt);
                        sdata = System.Text.Encoding.ASCII.GetBytes(sendrtxt.ToCharArray());
                        estrem.Write(sdata, 0, sdata.Length);

                        recvtxt = rstrem.ReadLine();
                        Console.WriteLine(recvtxt);
                        File.AppendAllText(logfile, Environment.NewLine + recvtxt);
                        sendrtxt = "RCPT TO: " + "<" + a + ">" + CRLF;
                        Console.Write(sendrtxt);
                        sdata = System.Text.Encoding.ASCII.GetBytes(sendrtxt.ToCharArray());
                        estrem.Write(sdata, 0, sdata.Length);

                        recvtxt = rstrem.ReadLine();
                        Console.WriteLine(recvtxt);
                        File.AppendAllText(logfile, Environment.NewLine + recvtxt);
                        sendrtxt = "DATA " + CRLF;
                        Console.Write(sendrtxt);
                        sdata = System.Text.Encoding.ASCII.GetBytes(sendrtxt.ToCharArray());
                        estrem.Write(sdata, 0, sdata.Length);

                        recvtxt = rstrem.ReadLine();
                        Console.WriteLine(recvtxt);
                        File.AppendAllText(logfile, Environment.NewLine + recvtxt);
                        sendrtxt = "SUBJECT: " + "Backup Summary" + CRLF + msg + CRLF + "." + CRLF;
                        Console.Write(sendrtxt);
                        sdata = System.Text.Encoding.ASCII.GetBytes(sendrtxt.ToCharArray());
                        estrem.Write(sdata, 0, sdata.Length);

                        recvtxt = rstrem.ReadLine();
                        Console.WriteLine(recvtxt);
                        File.AppendAllText(logfile, Environment.NewLine + recvtxt);
                        

                        

                    }
                    catch (InvalidOperationException se)
                    {
                        Console.WriteLine(se.ToString());
                        
                    }

                    
                }
                sendrtxt = "QUIT " + CRLF;
                Console.Write(sendrtxt);
                sdata = System.Text.Encoding.ASCII.GetBytes(sendrtxt.ToCharArray());
                estrem.Write(sdata, 0, sdata.Length);

                recvtxt = rstrem.ReadLine();
                Console.WriteLine(recvtxt);
                File.AppendAllText(logfile, Environment.NewLine + recvtxt);
                estrem.Close();
                rstrem.Close();
            }
            catch (Exception nc)
            {
                Console.WriteLine(@"Can not connect to " + MailHost);
                Console.WriteLine("No notifications sent");
                Console.WriteLine(nc.ToString());

            }
            }
        public void tracker(int dev, int timeleft, int logno, int misdev, int misdays, int rotdev, int failed)
        {
            StreamWriter updatemarker = new StreamWriter(@"C:\DRCTECH\DRCBackupSys.mrk");
            updatemarker.Write(dev);
            updatemarker.Write(updatemarker.NewLine + rotdev);
            updatemarker.Write(updatemarker.NewLine + timeleft);
            updatemarker.Write(updatemarker.NewLine + (logno));
            updatemarker.Write(updatemarker.NewLine + misdev);
            updatemarker.Write(updatemarker.NewLine + misdays);
            updatemarker.Write(updatemarker.NewLine + failed);
            updatemarker.Write(updatemarker.NewLine + @"<END>");
            updatemarker.Write(updatemarker.NewLine + "device number" + updatemarker.NewLine + "Rotation Device" + updatemarker.NewLine + "days left on device rotation" + updatemarker.NewLine + "lastlog number" + updatemarker.NewLine + "missed device -5=none" + updatemarker.NewLine + "days device was absent but backup performed on other device" + updatemarker.NewLine + "Backup failures for current sequence");
            updatemarker.Close();
            updatemarker.Dispose();
        }
        public string Name(string path)
        {
            return path.Remove(0, path.LastIndexOf('\\') + 1);
        }
        public void archiveoff(string arch)
        {
            bool Hid = (File.GetAttributes(arch) & FileAttributes.Hidden) == FileAttributes.Hidden;
            bool Ro = (File.GetAttributes(arch) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
            bool Comp = (File.GetAttributes(arch) & FileAttributes.Compressed) == FileAttributes.Compressed;
            bool Enc = (File.GetAttributes(arch) & FileAttributes.Encrypted) == FileAttributes.Encrypted;
            bool NCInd = (File.GetAttributes(arch) & FileAttributes.NotContentIndexed) == FileAttributes.NotContentIndexed;
            bool RepPon = (File.GetAttributes(arch) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
            bool SparF = (File.GetAttributes(arch) & FileAttributes.SparseFile) == FileAttributes.SparseFile;
            bool SysF = (File.GetAttributes(arch) & FileAttributes.System) == FileAttributes.System;
            bool TmpF = (File.GetAttributes(arch) & FileAttributes.Temporary) == FileAttributes.Temporary;
            File.SetAttributes(arch, FileAttributes.Normal);
            if (Hid) File.SetAttributes(arch, FileAttributes.Hidden);
            if (Ro) File.SetAttributes(arch, FileAttributes.ReadOnly);
            if (Comp) File.SetAttributes(arch, FileAttributes.Compressed);
            if (Enc) File.SetAttributes(arch, FileAttributes.Encrypted);
            if (NCInd) File.SetAttributes(arch, FileAttributes.NotContentIndexed);
            if (RepPon) File.SetAttributes(arch, FileAttributes.ReparsePoint);
            if (SparF) File.SetAttributes(arch, FileAttributes.SparseFile);
            if (SysF) File.SetAttributes(arch, FileAttributes.System);
            if (TmpF) File.SetAttributes(arch, FileAttributes.Temporary);

        }
        public void MakeNewMarkFile()
        {
            StreamWriter wmark = new StreamWriter(@"c:\DRCTECH\DrcBackupSys.mrk");
            wmark.WriteLine(0);
            wmark.WriteLine(0);
            wmark.WriteLine(7);
            wmark.WriteLine(0);
            wmark.WriteLine(-5);
            wmark.WriteLine(0);
            wmark.WriteLine(0);
            wmark.WriteLine("************************");
            wmark.WriteLine("last device number");
            wmark.WriteLine("Rotation Device");
            wmark.WriteLine("days left on device rotation");
            wmark.WriteLine("lastlog number");
            wmark.WriteLine("missed device -5=none");
            wmark.WriteLine("days device was absent but backup performed on other device");
            wmark.WriteLine("Backup failures for current sequence");
            wmark.Close();
            wmark.Dispose();
        }
        public string PathName(string path)
        {
            string PName = "";
            int idx = path.LastIndexOf('\\');
            if (path.Substring(0, 2) == @"\\") //path is share name
            {
                
                PName = path.Substring(2, idx-1);
            }
            else PName = @"Drive " + path.Substring(0, 1)+path.Substring(2,idx-1);
            return PName;
        }
        public string ConvertBytes(long BytesIn)
        {
            
            string BytesOut = BytesIn.ToString()+@" Bytes";
            double DBytesIn = Convert.ToDouble(BytesIn);
            
            if (BytesIn > 999999999999)
            {
                BytesOut = "TB";
                DBytesIn = DBytesIn * .000000000001;
            }
            else if (BytesIn > 999999999)
            {
                BytesOut = "GB";
                DBytesIn = DBytesIn * .000000001;
            }
            else if (BytesIn > 999999)

            {
                BytesOut = "MB";
                DBytesIn = DBytesIn * .000001;
            }
            else if (BytesIn > 999)
            {
                BytesOut = "KB";
                DBytesIn = DBytesIn * .001;
            }
            if (BytesOut.Length < 3)//bytes out changed from Bytes to TB or KB etc so greater than 999 bytes: round up and change displayed amounts to match.
            {
                DBytesIn = Math.Round(DBytesIn, 3);
                BytesOut = DBytesIn.ToString() + BytesOut + @" (" + BytesIn.ToString() + @" Bytes)";
            }
            return BytesOut;
        }
    }
}
