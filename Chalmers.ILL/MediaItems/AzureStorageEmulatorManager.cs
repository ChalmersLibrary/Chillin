using System;
using System.Diagnostics;
using System.Security.Principal;

namespace Chalmers.ILL.MediaItems
{
    public class AzureStorageEmulatorManager
    {
        private const string _windowsAzureStorageEmulatorPath = @"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe";

        public static void Start()
        {
            if (IsAdministrator())
            {
                InstallStorageEmulatorIfNotAlreadyInstalled();

                InitializeStorageEmulator();

                StartStorageEmulator();
            }
            else
            {
                throw new Exception("Need to run Visual Studio as administrator to be able to use the Azure Storage Emulator.");
            }
        }

        public static void Stop()
        {
            StopStorageEmulator();
        }

        public static void Clear()
        {
            ClearStorageEmulator();
        }

        #region Private methods

        private static bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                    .IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void InstallStorageEmulatorIfNotAlreadyInstalled()
        {
            using (Process process = Process.Start(new ProcessStartInfo { FileName = "WebPICMD", Arguments = @"/Install /Products:""WindowsAzureStorageEmulator.4.3"" /AcceptEula" }))
            {
                process.WaitForExit();
            }
        }

        private static void InitializeStorageEmulator()
        {
            using (Process process = Process.Start(CreateEmulatorStartInfo("init")))
            {
                process.WaitForExit();
            }
        }

        private static void StartStorageEmulator()
        {
            using (Process process = Process.Start(CreateEmulatorStartInfo("start")))
            {
                process.WaitForExit();
            }
        }

        private static void StopStorageEmulator()
        {
            using (Process process = Process.Start(CreateEmulatorStartInfo("stop")))
            {
                process.WaitForExit();
            }
        }

        private static void ClearStorageEmulator()
        {
            using (Process process = Process.Start(CreateEmulatorStartInfo("clear")))
            {
                process.WaitForExit();
            }
        }

        private static ProcessStartInfo CreateEmulatorStartInfo(string args)
        {
            return new ProcessStartInfo
            {
                FileName = _windowsAzureStorageEmulatorPath,
                Arguments = args
            };
        }

        #endregion
    }
}