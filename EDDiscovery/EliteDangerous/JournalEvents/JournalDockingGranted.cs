﻿/*
 * Copyright © 2016 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Fronter Developments plc.
 */
using Newtonsoft.Json.Linq;
using System.Linq;

namespace EDDiscovery.EliteDangerous.JournalEvents
{
    //When written: when landing at landing pad in a space station, outpost, or surface settlement
    //Parameters:
    //•	StationName: name of station
    //•	StationType: type of station
    //•	StarSystem: name of system
    //•	CockpitBreach:true (only if landing with breached cockpit)
    //•	Faction: station’s controlling faction
    //•	FactionState
    //•	Allegiance
    //•	Economy
    //•	Government
    //•	Security
    public class JournalDockingGranted : JournalEntry
    {
        public JournalDockingGranted(JObject evt ) : base(evt, JournalTypeEnum.DockingGranted)
        {
            StationName = JSONHelper.GetStringDef(evt["StationName"]);
            LandingPad = JSONHelper.GetInt(evt["LandingPad"]);
        }
        public string StationName { get; set; }
        public int LandingPad { get; set; }

        public static System.Drawing.Bitmap Icon { get { return EDDiscovery.Properties.Resources.dockinggranted; } }

    }
}
