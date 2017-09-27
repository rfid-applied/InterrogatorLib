using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;

namespace InterrogatorLib
{
#if NET35_CF
	/// <summary>
	/// Platform-specific utility functions
	/// </summary>
	class PlatformUtility
    {
        [DllImport("Coredll.dll")]
        public static extern void MessageBeep(uint BeepType);

        public enum MB
        {
            MB_ICONEXCLAMATION = 0x00000030,
            MB_ICONERROR = 0x00000010,
            MB_OK = 0x0
        };

        [DllImport("Coredll.dll", EntryPoint = "PlaySound", CharSet = CharSet.Auto)]
        public static extern int PlaySound(String pszSound, int hmod, int falgs);

        public enum SND
        {
            SND_SYNC = 0x0000,/* play synchronously (default) */
            SND_ASYNC = 0x0001, /* play asynchronously */
            SND_NODEFAULT = 0x0002, /* silence (!default) if sound not found */
            SND_MEMORY = 0x0004, /* pszSound points to a memory file */
            SND_LOOP = 0x0008, /* loop the sound until next sndPlaySound */
            SND_NOSTOP = 0x0010, /* don't stop any currently playing sound */
            SND_NOWAIT = 0x00002000, /* don't wait if the driver is busy */
            SND_ALIAS = 0x00010000,/* name is a registry alias */
            SND_ALIAS_ID = 0x00110000, /* alias is a pre d ID */
            SND_FILENAME = 0x00020000, /* name is file name */
            SND_RESOURCE = 0x00040004, /* name is resource name or atom */
            SND_PURGE = 0x0040,  /* purge non-static events for task */
            SND_APPLICATION = 0x0080,  /* look for application specific */
        };

        [DllImport("coredll.dll")]
        public static extern IntPtr CreateMutex(IntPtr lpMutexAttributes, bool initialOwner, string lpName);

        [DllImport("coredll.dll")]
        public static extern bool ReleaseMutex(IntPtr hMutex);

        [DllImport("coredll.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("coredll.dll", SetLastError = true)]
        public static extern Int32 WaitForSingleObject(IntPtr Handle, Int32 Wait);

        public const int WAIT_OBJECT_0 = 0;

        [DllImport("coredll.dll")]
        static extern int SHGetSpecialFolderPath(IntPtr hwndOwner, StringBuilder lpszPath, int nFolder, int fCreate);

        const int CSIDL_APPDATA = 0x001A;

        public static string GetAppDataPath()
        {
            var path = new StringBuilder(255);
            SHGetSpecialFolderPath(IntPtr.Zero, path, CSIDL_APPDATA, 1);
            return path.ToString();
        }

        // Returns GUID as set in Properties of the application
        public static string GetApplicationGUID()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var attribute = (GuidAttribute)assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
            return attribute.Value;
        }

        public static IntPtr GrabApplicationMutex()
        {
            return CreateMutex(IntPtr.Zero, false, String.Format("Global\\{0}", GetApplicationGUID()));
        }

        public static void ReleaseApplicationMutex(IntPtr appMutex)
        {
            ReleaseMutex(appMutex);
            CloseHandle(appMutex);
        }
    }
#endif
}
