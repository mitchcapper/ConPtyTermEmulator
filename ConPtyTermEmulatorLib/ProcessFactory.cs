using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Threading;
using Windows.Win32.Security;
using Windows.Win32.Foundation;

namespace ConPtyTermEmulatorLib
{
    /// <summary>
    /// Support for starting and configuring processes.
    /// </summary>
    /// <remarks>
    /// Possible to replace with managed code? The key is being able to provide the PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE attribute
    /// </remarks>
    static class ProcessFactory
    {
        /// <summary>
        /// Start and configure a process. The return value represents the process and should be disposed.
        /// </summary>
        internal static Process Start(string command, nuint attributes, IntPtr hPC)
        {
            var startupInfo = ConfigureProcessThread(hPC, attributes);
            var processInfo = RunProcess(ref startupInfo, command);
            return new Process(startupInfo, processInfo);
        }

       unsafe private static STARTUPINFOEXW ConfigureProcessThread(IntPtr hPC, nuint attributes)
        {
            // this method implements the behavior described in https://docs.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session#preparing-for-creation-of-the-child-process

            nuint lpSize = 0;
            var success = PInvoke.InitializeProcThreadAttributeList(
                lpAttributeList: default,
                dwAttributeCount: 1,
                dwFlags: 0,
                lpSize: &lpSize
            );
            if (success || lpSize == 0) // we're not expecting `success` here, we just want to get the calculated lpSize
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not calculate the number of bytes for the attribute list.");
            }

            var startupInfo = new STARTUPINFOEXW();
            startupInfo.StartupInfo.cb = (uint)Marshal.SizeOf<STARTUPINFOEXW>();
            startupInfo.lpAttributeList =new LPPROC_THREAD_ATTRIBUTE_LIST( (void*)Marshal.AllocHGlobal((int)lpSize));

            success = PInvoke.InitializeProcThreadAttributeList(
                lpAttributeList: startupInfo.lpAttributeList,
                dwAttributeCount: 1,
                dwFlags: 0,
                lpSize: &lpSize
            );
            if (!success)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not set up attribute list.");
            }

            success = PInvoke.UpdateProcThreadAttribute(
                lpAttributeList: startupInfo.lpAttributeList,
                dwFlags: 0,
                attributes,
                (void*)hPC,
                (nuint) IntPtr.Size,
                null,
                (nuint*)null
            );
            if (!success)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not set pseudoconsole thread attribute.");
            }

            return startupInfo;
        }

        unsafe private static PROCESS_INFORMATION RunProcess(ref STARTUPINFOEXW sInfoEx, string commandLine)
        {
            uint securityAttributeSize =(uint) Marshal.SizeOf<SECURITY_ATTRIBUTES>();
            var pSec = new SECURITY_ATTRIBUTES { nLength = securityAttributeSize };
            var tSec = new SECURITY_ATTRIBUTES { nLength = securityAttributeSize };
            var info = sInfoEx;
            fixed (char* spancommandLine = commandLine.ToCharArray()) {
                PROCESS_INFORMATION pInfo = default;
                PWSTR cli = new PWSTR((char*)spancommandLine);

                var success = PInvoke.CreateProcess(
                    lpApplicationName: null,
                    cli,
                    lpProcessAttributes: &pSec,
                    lpThreadAttributes: &tSec,
                    bInheritHandles: false,
                    dwCreationFlags: PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT,
                    lpEnvironment: null,
                    lpCurrentDirectory: null,
                    lpStartupInfo: (STARTUPINFOW*)&info,//this could be quite wrong may need to do point verison for everyhting
                    lpProcessInformation: &pInfo
                );

                if (!success) {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create process.");
                }
                return pInfo;
            }
        }
    }
}
