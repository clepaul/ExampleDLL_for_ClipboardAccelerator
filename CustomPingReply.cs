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
using System.Net.NetworkInformation;

namespace CA_DLLTest
{
    public class CustomPingReply
    {
        public string InputAddress { get; set; }

        public IPStatus Status { get; set; }

        public System.Net.IPAddress Address { get; set; }

        public long RoundtripTime { get; set; }

        // PingOptions does not provide any usefull information
        // public PingOptions Options { get; set; }

        public int TaskId { get; set; }

        // public byte[] Buffer { get; set; }
    }

    public class DummyPingReply
    {
        public string InputAddress { get; set; }

        public string Status { get; set; }

        public string Address { get; set; }

        public long RoundtripTime { get; set; }
        
        // PingOptions does not provide any usefull information
        //public string Options { get; set; }

        public int TaskId { get; set; }
    }
}
