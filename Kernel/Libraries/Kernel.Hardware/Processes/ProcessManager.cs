﻿#define PROCESSMANAGER_TRACE
#undef PROCESSMANAGER_TRACE

using System;
using Kernel.FOS_System.Collections;

namespace Kernel.Hardware.Processes
{
    public static unsafe class ProcessManager
    {
        public static List Processes = new List();
        public static Process CurrentProcess = null;
        public static Thread CurrentThread = null;
        public static ThreadState* CurrentThread_State = null;

        private static uint ProcessIdGenerator = 1;

        public static Process CreateProcess(ThreadStartMethod MainMethod, FOS_System.String Name, bool UserMode)
        {
//#if PROCESSMANAGER_TRACE
            BasicConsole.WriteLine("Creating process object...");
//#endif
            return new Process(MainMethod, ProcessIdGenerator++, Name, UserMode);
        }
        public static void RegisterProcess(Process process, Scheduler.Priority priority)
        {
#if PROCESSMANAGER_TRACE
            BasicConsole.WriteLine("Registering process...");
            BasicConsole.WriteLine("Disabling scheduler...");
#endif
            bool reenable = Scheduler.Enabled;
            if (reenable)
            {
                Scheduler.Disable();
            }
#if PROCESSMANAGER_TRACE
            BasicConsole.WriteLine("Initialising process...");
#endif
            Scheduler.InitProcess(process, priority);

#if PROCESSMANAGER_TRACE
            BasicConsole.WriteLine("Adding process...");
#endif
            Processes.Add(process);

#if PROCESSMANAGER_TRACE
            BasicConsole.WriteLine("Enabling scheduler...");
#endif
            if (reenable)
            {
                Scheduler.Enable();
            }
        }

        /// <remarks>
        /// Specifying threadId=-1 accepts any thread from the specified process.
        /// No guarantees are made about the thread chosen. This is used when you
        /// mainly want to switch process context and don't care about the specific
        /// thread context e.g. during an interrupt.
        /// </remarks>
        public static void SwitchProcess(uint processId, int threadId)
        {
            //Switch the current memory layout across.
            //  Don't touch register state etc, just the memory layout

            bool dontSwitchOutIn = false;

            if (CurrentProcess != null &&
                CurrentProcess.Id == processId)
            {
                if (CurrentThread != null &&
                    (CurrentThread.Id == threadId || threadId == -1))
                {
                    return;
                }
                else
                {
                    dontSwitchOutIn = true;
                }
            }

            if (!dontSwitchOutIn)
            {
                //BasicConsole.WriteLine("Switching out: " + CurrentProcess.Name);
                CurrentProcess.SwitchOut();

                CurrentProcess = null;
                
                for (int i = 0; i < Processes.Count; i++)
                {
                    if (((Process)Processes[i]).Id == processId)
                    {
                        CurrentProcess = ((Process)Processes[i]);
                        break;
                    }
                }

                // Process not found
                if (CurrentProcess == null)
                {
                    BasicConsole.WriteLine("Process not found.");
                    return;
                }
                //BasicConsole.WriteLine("Process found. " + CurrentProcess.Name);
            }

            CurrentThread = null;
            CurrentThread_State = null;

            if (threadId == -1)
            {
                if (CurrentProcess.Threads.Count > 0)
                {
                    CurrentThread = (Thread)CurrentProcess.Threads[0];
                }
            }
            else
            {
                for (int i = 0; i < CurrentProcess.Threads.Count; i++)
                {
                    if (((Thread)CurrentProcess.Threads[i]).Id == threadId)
                    {
                        CurrentThread = (Thread)CurrentProcess.Threads[i];
                        break;
                    }
                }
            }

            // No threads in the process (?!) or process not found
            if (CurrentThread == null)
            {
                BasicConsole.WriteLine("Thread not found.");
                return;
            }
            //BasicConsole.WriteLine("Thread found.");

            CurrentThread_State = CurrentThread.State;
            //BasicConsole.WriteLine("Thread state updated.");
            
            if (!dontSwitchOutIn)
            {
                //BasicConsole.WriteLine("Switching in: " + CurrentProcess.Name);
                CurrentProcess.SwitchIn();
            }
        }
    }
}