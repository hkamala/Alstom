using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConflictManagementLibrary.Helpers;
using ConflictManagementLibrary.Logging;
using static ConflictManagementLibrary.Helpers.GlobalDeclarations;

namespace ConflictManagementLibrary.Management
{
    public class AppArchiveManager
    {
        public static AppArchiveManager CreateInstance(string theArchivePath, IMyLogger theLogger)
        {
            return new AppArchiveManager(theArchivePath,  theLogger);
        }

        private readonly string _theArchivePath;
        private readonly IMyLogger? _theLogger;
        public List<AppArchiveEntry> MyArchiveEntries = new List<AppArchiveEntry>();
        private bool _beginArchiving;
        private DateTime LastArchiveEvent = DateTime.Now;

        private AppArchiveManager(string theArchivePath, IMyLogger theLogger)
        {
            _theArchivePath = theArchivePath;
            _theLogger = theLogger;
            //CreateEntries(theConfigurationFile.MyApplicationArchiveDirectories);
            CreateEntriesManually();
            ForceArchive();
            Thread newThread = new Thread(DoArchiving);
            newThread.Start();
        }

        private void ForceArchive()
        {
            try
            {
                DoArchiveEntries();
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void DoArchiving()
        {
            _beginArchiving = true;

            while (true)
            {
                try
                {
                    var theCurrentTime = DateTime.Now;
                    //var currentSecond = theCurrentTime.ToString("ss");
                    //if (currentSecond == "00")
                    //{
                    //    var currentTime = theCurrentTime.ToString("HH:mm:ss");
                    //    if (currentTime == _theConfigurationFile.MyApplicationArchiveTime + ":00")
                    //    {
                    //        DoArchiveEntries();
                    //    }
                    //}
                    if (theCurrentTime.AddHours(-MyArchiveEventInHours) > LastArchiveEvent)
                    {
                        LastArchiveEvent = DateTime.Now;
                        DoArchiveEntries();
                    }
                }
                catch (Exception e)
                {
                    _theLogger?.LogException(e.ToString());
                }
                Thread.Sleep(5000);
            }
            // ReSharper disable once FunctionNeverReturns
        }
        public void DoArchiveLogFiles()
        {
            try
            {
                var theEntry = GetEntry("LogFiles");
                DoArchiveFiles(theEntry);

            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private void DoArchiveEntries()
        {
            foreach (var e in MyArchiveEntries)
            {
                DoArchiveFiles(e);
            }
        }
        private AppArchiveEntry GetEntry(string theName)
        {
            foreach (var e in MyArchiveEntries)
            {
                if (e.TheEntryName == theName) return e;
            }

            return null;
        }

        private void CreateEntriesManually()
        {
            MyArchiveEntries.Add(AppArchiveEntry.CreateInstance("LogFilesCMLibrary", @"\logs\"));
            //MyArchiveEntries.Add(AppArchiveEntry.CreateInstance("LogFilesCMService", @"\LOG\"));
            MyArchiveEntries.Add(AppArchiveEntry.CreateInstance("SerializedFiles", @"\Data\SerializeData"));
        }
        private void CreateEntries(string theData)
        {
            try
            {

                string[] theDirectories = theData.Split(',');
                foreach (var d in theDirectories)
                {
                    string[] data = d.Split(':');
                    try
                    {
                        MyArchiveEntries.Add(AppArchiveEntry.CreateInstance(data[0], data[1]));
                    }
                    catch (Exception ex)
                    {
                        _theLogger?.LogException(ex.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
        }
        private string CreateArchivePath(AppArchiveEntry theArchiveEntry)
        {
            try
            {
                if (!Directory.Exists(_theArchivePath)) Directory.CreateDirectory(_theArchivePath);
                return _theArchivePath + @"\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-Archive-" + theArchiveEntry.TheEntryName + ".zip";

            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }

            return string.Empty;
        }
        private void DoArchiveFiles(AppArchiveEntry theArchiveEntry)
        {
            try
            {
                if (theArchiveEntry == null) return;
                string startPath = GlobalDeclarations.GetExecutingDirectoryName() + theArchiveEntry.TheFolderPath;
                //if (Directory.GetFiles(startPath).Length == 0) return;
                string zipPath = CreateArchivePath(theArchiveEntry);
                ZipFile.CreateFromDirectory(startPath, zipPath);
                RemoveArchiveFolder(theArchiveEntry);
                _theLogger?.LogInfo("This File Directory Archived (" + theArchiveEntry.TheEntryName + " @ " + theArchiveEntry.TheFolderPath + ")");

            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }

        }
        private void RemoveArchiveFolder(AppArchiveEntry theEntry)
        {
            var files = Directory.GetFiles(GetExecutingDirectoryName() + theEntry.TheFolderPath);
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                    //var filePath = GetExecutingDirectoryName() + theEntry.TheFolderPath + @"\" + file;
                    //File.Delete(filePath);
                }
                catch (Exception e)
                {
                    _theLogger?.LogException(e.ToString());
                }
            }

            try
            {
                Directory.Delete(GetExecutingDirectoryName() + theEntry.TheFolderPath, true);
            }
            catch (Exception e)
            {
                _theLogger?.LogException(e.ToString());
            }
            
        }
    }
}
