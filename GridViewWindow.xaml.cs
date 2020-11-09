/* Example DLL to extend the tool "Clipboard Accelerator"
Copyright (C) 2016 - 2020  Clemens Paul

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.NetworkInformation;
using System.Threading;
using System.Net.Sockets;

namespace CA_DLLTest
{    
    // Source: https://stackoverflow.com/questions/3573339/no-creation-of-a-wpf-window-in-a-dll-project
    public partial class GridViewWindow : Window
    {
        public GridViewWindow(string[] ClipboardText, XMLRecord xmlRecord)
        {
            InitializeComponent();

            PingLoop(ClipboardText.ToList(), xmlRecord.PingCallDelay);

            // Populate the gridview with initial data
            List<DummyPingReply> dummyData = new List<DummyPingReply>();

            foreach (string ct in ClipboardText)
            {
                DummyPingReply cPingReply = new DummyPingReply() { Status = "Pinging...", Address = "", RoundtripTime = 0, InputAddress = ct, TaskId = 0};
                dummyData.Add(cPingReply);
            }
            
            dataGrid.ItemsSource = dummyData;

           
            textBoxAdditionalText.Text = xmlRecord.CustomMessage;

            


            /* // The following would be a ping loop in the UI thread with no async / thread logic
            List <CustomPingReply> preply = new List<CustomPingReply>();

            Ping pg = new Ping();
            foreach (string ct in ClipboardText)
            {
                try { 
                    PingReply pr = pg.Send(ct);
                    CustomPingReply cpr = new CustomPingReply() { Status = pr.Status, Address = pr.Address, RoundtripTime = pr.RoundtripTime, Options = pr.Options, InputAddress = ct };
                    preply.Add(cpr);
                }
                catch(Exception exc) {
                    //MessageBox.Show(exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);                    
                    CustomPingReply creply = new CustomPingReply() { Status = (IPStatus)(-1), Address = null, RoundtripTime = 0, Options = null,  InputAddress = ct};
                    preply.Add(creply);
                }               
            }            
            dataGrid.ItemsSource = preply;  */
        }


        // Source: https://stackoverflow.com/questions/22078510/how-can-i-make-many-pings-asynchronously-at-the-same-time
        public async void PingLoop(List<string> cpyClipboardText, int pingCallDelay)
        {
            Task<List<PingReply>> pingAsyncTask = PingAsync(cpyClipboardText, pingCallDelay);

            List<PingReply> result = await pingAsyncTask;            
            
            // dataGrid.ItemsSource = result; // <- this was active before moving the CutomPingReply logic to the "PingAsync" method
        }


        // Source: https://stackoverflow.com/questions/22078510/how-can-i-make-many-pings-asynchronously-at-the-same-time
        private async Task<List<PingReply>> PingAsync(List<string> cpyClipboardText, int pingCallDelay)
        {            
            List<CustomPingReply> pingReplyList = new List<CustomPingReply>();
            List<Task<PingReply>> tasks = new List<Task<PingReply>>();

            
            foreach (string theAddress in cpyClipboardText)
            {
                // To be checked: The pingTimeout seems to be not always working as described here: https://superuser.com/questions/1154314/ping-ignoring-w-timeout-switch
                int pingTimeout = 1500;
                Ping pingSender = new Ping();
                var task = pingSender.SendPingAsync(theAddress, pingTimeout);
                tasks.Add(task);

                // Fill up the datagrid befor the ping has happened with just the target host name
                CustomPingReply cPingReply = new CustomPingReply() { Status = (IPStatus)(-1), Address = null, RoundtripTime = 0, InputAddress = theAddress, TaskId = task.Id };
                pingReplyList.Add(cPingReply);

                // Sleep for a short beriod of time to not start to many pings as the same time
                // Source: https://stackoverflow.com/questions/13429707/how-to-get-awaitable-thread-sleep
                // Todo:  document the xml property of the pingCallDelay
                await Task.Delay(pingCallDelay);
            }            


            // Needs to be defined here to make try/catch working - "results" must be in scope of all below
            PingReply[] results = null;
            
            try
            {
                // If a "SendPingAsync" call fails it does not return a "PingReply" but it throws an exception, therefore this try/catch is required
                results = await Task.WhenAll(tasks);                              
            }
            catch (PingException exc) // catch(Exception exc)
            {
                //MessageBox.Show("exception.message: " + exc.Message + "\r\ninnerexception: " + exc.InnerException + "\r\ndata: " + exc.Data.ToString() + "\r\nstack: " + exc.StackTrace);

                List<Task<PingReply>> tasksToRemove = new List<Task<PingReply>>();

                foreach (Task<PingReply> theTask in tasks)
                {
                    if (theTask.IsFaulted == true)
                    {                        
                        tasksToRemove.Add(theTask);                                             
                    }
                }

                // Remove the failed tasks from the "tasks" list here and not in the foreach loop above 
                // because this will remove it from the list during looping through this list and cause another exception.
                foreach (Task<PingReply> theRemoveTask in tasksToRemove)
                {
                    tasks.Remove(theRemoveTask);
                    theRemoveTask.Dispose();                    
                }
                
                // Call "WhenAll" a second time to get all successfully completed SendPingAsync results
                results = await Task.WhenAll(tasks);                              
            }


            // Loop through all tasks<results> and populate the "pingReplyList" with the actual replies
            // Source: https://stackoverflow.com/questions/19280986/best-way-to-update-an-element-in-a-generic-list
            foreach (Task<PingReply> currTask in tasks)
            {
                var rep = pingReplyList.First(p => p.TaskId == currTask.Id);
                rep.Status = currTask.Result.Status;
                rep.RoundtripTime = currTask.Result.RoundtripTime;
                // The below "Options" das not provide any usefull information
                // rep.Options = currTask.Result.Options;
                rep.Address = currTask.Result.Address;                
            }


            // Feed the dataGrid with the actual list of PingReply
            dataGrid.ItemsSource = pingReplyList;

            // Remove the "TaskId" column: https://www.c-sharpcorner.com/uploadfile/dpatra/hide-un-hide-datagrid-columns-in-wpf/
            dataGrid.Columns.Remove(dataGrid.Columns.First(c => c.Header.ToString() == "TaskId"));

            return results.ToList();            
        }
    }
}