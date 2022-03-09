using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using System.Threading;

namespace ClockPlus
{
    class ComputerInformation
    {
        //CPU
        private Dictionary<string, PerformanceCounter> performanceCounters = new Dictionary<string, PerformanceCounter>();
        private List<string> networkCards = new List<string>();
        int totalNumberOfNetAdapters;
        int cpuMaxFrequency = (int)Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "~MHz", 0);
        int ramTotal = Convert.ToInt32(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1048576);

        NAudio.CoreAudioApi.MMDeviceEnumerator devEnum = new NAudio.CoreAudioApi.MMDeviceEnumerator();
        NAudio.CoreAudioApi.MMDevice defaultDevice;
        private static ComputerInformation instance = null;
        public static ComputerInformation Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ComputerInformation();
                    instance.defaultDevice = instance.devEnum.GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.Role.Multimedia);
                }
                return instance;
            }
        }
        private ComputerInformation()
        {
            performanceCounters.Add("cpuFreq",new PerformanceCounter("Processor Information", "% of Maximum Frequency", "0,_Total"));
            performanceCounters.Add("cpuLimit", new PerformanceCounter("Processor Information", "% Performance Limit", "_Total"));
            performanceCounters.Add("cpuPercent", new PerformanceCounter("Processor Information", "% Processor Time", "_Total"));
            performanceCounters.Add("ram", new PerformanceCounter("Memory", "Available MBytes"));
            performanceCounters.Add("drivePercent", new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total"));
            performanceCounters.Add("driveRead", new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total"));
            performanceCounters.Add("driveWrite", new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total"));


            const string networkCardsPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkCards";//note backslash
            using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(networkCardsPath))
            {
                foreach (string networkCard in registryKey.GetSubKeyNames())
                {
                    networkCards.Add(
                        Convert.ToString(
                        Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkCards\"+ networkCard, "Description", 0)
                        )
                        .Replace("(", "[").Replace(")", "]").Replace("/", "_")
                        );
                }
            }
            int i = 0;
            foreach (string networkCard in networkCards)
            {
                
                performanceCounters.Add("netSend "+i, new PerformanceCounter("Network Interface", "Bytes Sent/sec", networkCard));
                performanceCounters.Add("netReceive "+i, new PerformanceCounter("Network Interface", "Bytes Received/sec", networkCard));
                i++;
            }
            totalNumberOfNetAdapters = i;
        }
        public double getPerformanceCounterValue(string stat)
        {
            PerformanceCounter counter;
            performanceCounters.TryGetValue(stat, out counter);
            return counter.NextValue();
        }
        public float getValue(string stat)
        {
            switch (stat)
            {
                case "cpuFreq": return semaphorize(stat, (x) => { return Convert.ToSingle(Math.Round(getPerformanceCounterValue("cpuFreq") * cpuMaxFrequency / 100.0)); });
                case "cpuLimit": return semaphorize(stat, (x) => { return Convert.ToSingle(getPerformanceCounterValue("cpuLimit")); });
                case "cpuPercent": return semaphorize(stat, (x) => { return Convert.ToSingle(getPerformanceCounterValue("cpuPercent") / 100.0); });
                case "ramTotal":return ramTotal;
                case "ramUsed": return ramTotal - (int)getPerformanceCounterValue("ram");
                case "ramPercent": return getValue("ramUsed") / getValue("ramTotal");
                case "driveRead": return semaphorize(stat, (x) => { return Convert.ToSingle(Math.Round(getPerformanceCounterValue("driveRead") / 1048576, 1)); }); 
                case "driveWrite": return semaphorize(stat, (x) => { return Convert.ToSingle(Math.Round(getPerformanceCounterValue("driveWrite") / 1048576, 1)); });
                case "drivePercent": return semaphorize(stat, (x) => { return Convert.ToSingle(Math.Min(getPerformanceCounterValue("drivePercent") / 100, 1)); });
                case "drivePercent100": return getValue("drivePercent") * 100;
                case "batteryPercent": return (SystemInformation.PowerStatus.BatteryLifePercent);
                case "batteryPercent100": return (int)(SystemInformation.PowerStatus.BatteryLifePercent*100);
                case "charging": return Convert.ToSingle(SystemInformation.PowerStatus.BatteryChargeStatus.ToString().Contains("Charging"));
                case "netSend": return semaphorize(stat, (x) => { return getNetworkSend(); });
                case "netReceive": return semaphorize(stat, (x) => { return getNetworkReceive(); });
                case "netSendMbps": return getValue("netSend")*1024;
                case "netReceiveMbps": return getValue("netReceive") * 1024;
                case "clockHour": return DateTime.Now.Hour;
                case "clockHourPercent": return Convert.ToSingle(DateTime.Now.Hour / 24.0);
                case "clockMinute": return DateTime.Now.Minute;
                case "clockMinutePercent": return Convert.ToSingle(DateTime.Now.Minute / 60.0);
                case "clockSecond": return DateTime.Now.Second;
                case "clockSecondPercent": return Convert.ToSingle(DateTime.Now.Second / 60.0);
                case "clockSecondPrecise": return Convert.ToSingle(DateTime.Now.Second + DateTime.Now.Millisecond / 1000.0);
                case "clockMillisecond": return Convert.ToSingle(DateTime.Now.Millisecond);
                case "clockSecondsInMillisecond": return Convert.ToSingle(DateTime.Now.Second * 1000) + Convert.ToSingle(DateTime.Now.Millisecond);
                case "clockSecondPrecisePercent": return Convert.ToSingle((DateTime.Now.Second + DateTime.Now.Millisecond/1000.0)/ 60.0);
                case "dateDayOfWeek": return ((((int)DateTime.Now.DayOfWeek) - 1) % 7);
                case "dateDayOfWeekPercent": return Convert.ToSingle(((((int)DateTime.Now.DayOfWeek) - 1) % 7) / 7);
                case "dateDay": return DateTime.Now.Day;
                case "dateDayPercent": return Convert.ToSingle(DateTime.Now.Day *1.0/ DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
                case "dateMonth": return DateTime.Now.Month;
                case "dateMonthPercent": return Convert.ToSingle(DateTime.Now.Month / 12.0);
                case "volumePeak": return defaultDevice.AudioMeterInformation.MasterPeakValue;
                case "volumePeakCapped": return getValue("volumePeak")* getValue("volumePercentDecibels");
                case "volumeLevelDecibels": return Convert.ToSingle(Math.Round(defaultDevice.AudioEndpointVolume.MasterVolumeLevel,2));
                case "volumePercentDecibels": return (1 - getValue("volumeLevelDecibels") / defaultDevice.AudioEndpointVolume.VolumeRange.MinDecibels);
                case "batteryCharging": return Convert.ToSingle(SystemInformation.PowerStatus.BatteryChargeStatus.ToString().Contains("Charging"));
                default: return 0;
                 //https://stackoverflow.com/questions/1195112/how-can-i-get-the-cpu-temperature
            }
        }
        Dictionary<string, Semaphore> semaphoreDict = new Dictionary<string, Semaphore>();
        Dictionary<string, float> semaphoreHoldingValuesDict = new Dictionary<string, float>();
        private float semaphorize(string key, Func<float, float> f)
        {
            if (!semaphoreDict.ContainsKey(key)) { semaphoreDict.Add(key, new Semaphore(initialCount: 1, maximumCount: 1)); }
            if (!semaphoreHoldingValuesDict.ContainsKey(key)) { semaphoreHoldingValuesDict.Add(key, 0); }
            Semaphore semaphoreObject = semaphoreDict[key];
            if (semaphoreObject.WaitOne(0))
            {
                semaphoreHoldingValuesDict[key] = f(0);
                releaseFromSemaphore(semaphoreObject, 100);
            }
            return semaphoreHoldingValuesDict[key];
        }
        private void releaseFromSemaphore(Semaphore semaphoreObject, int time)
        {
            Thread t = new Thread((x) => {
                Thread.Sleep(time);
                semaphoreObject.Release();
            });
            t.IsBackground = true;
            t.Start(semaphoreObject);
        }
        public string getNetAdapters()
        {
            string ret="";
            foreach (string networkCard in networkCards)
            {
                ret = ret + networkCard+"\n";
            }
            return ret;
        }
        public float getNetworkSend()
        {
            float ret = 0;
            for (int i = 0; i < totalNumberOfNetAdapters; i++)
            {
                try
                {
                    ret = ret + Convert.ToSingle(Math.Round(Convert.ToSingle(getPerformanceCounterValue("netSend " + i) / 134217728), 2));
                } catch (InvalidOperationException e) { }
            }
            return ret;
        }

        public float getNetworkReceive()
        {
            float ret = 0;
            for (int i = 0; i < totalNumberOfNetAdapters; i++)
            {
                try {
                    ret = ret + Convert.ToSingle(Math.Round(Convert.ToSingle(getPerformanceCounterValue("netReceive " + i) / 134217728), 2));
                }
                catch (InvalidOperationException e) { }
            }
            return ret;
        }
    }
}
