using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using Sandbox.ModAPI;
using VRageMath;
using VRage;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Game.Components;
using VRage.Game;
using VRage.Game.ModAPI;
using System.Collections.Concurrent;
using Sandbox.Game.EntityComponents;
using VRage.Game.Entity;

using ProtoBuf;


namespace SED {
	
	[ProtoContract]
	//worldConstants savefile
	public class Constants {
		
		[ProtoMember(1)]
		public int advanceTime = 432000;
		
		[ProtoMember(2)]
		public int gridSize = 15;
		
		[ProtoMember(3)]
		public int tileSize = 500000;
		
		[ProtoMember(4)]
		public long compensation = 25000000;
		
		[ProtoMember(5)]
		public int capturePoints = 30;
		
		[ProtoMember(6)]
		public bool compatibilityMode = false;
		
		[ProtoMember(7)]
		public bool forceAlliesFriendly = false;
		
		[ProtoMember(8)]
		public bool allowDynamicMap = true;
		
		[ProtoMember(9)]
		public int minPlayers = 1;
		
		public Constants(){
			
		}
		
	}
	
	//manager for above saveFile
	public class constantManager {
		
		//filename
		private string fileName = "warConstants.cfg";
		
		private Core core;
		
		//data
		/*public int advanceTime;
		public int gridSize;
		public int tileSize;
		public long payment;
		public int capturePoints;
		public bool compatibilityMode;
		public bool forceAlliesFriendly;
		public bool allowDynamicMap;
		public int minPlayers;*/
		
		//saveFile
		public Constants constants = new Constants();
		
		//initializer
		public constantManager(Core process){
			core = process;
			load();
			//update();
		}
		
		//fills in saveFile
		/*public void update(){
			advanceTime = constants.advanceTime;
			gridSize = constants.gridSize;
			tileSize = constants.tileSize;
			payment = constants.compensation;
			capturePoints = constants.capturePoints;
			compatibilityMode = constants.compatibilityMode;
			forceAlliesFriendly = constants.forceAlliesFriendly;
			allowDynamicMap = constants.allowDynamicMap;
			minPlayers = constants.minPlayers;
		}*/
		
		//savefile loader
		public void load(){
			
			if(MyAPIGateway.Utilities.FileExistsInWorldStorage(fileName, typeof(Constants))){
				
				TextReader file = MyAPIGateway.Utilities.ReadFileInWorldStorage(fileName, typeof(Constants));
                string contents = file.ReadToEnd();
                file.Close();
				
				constants = MyAPIGateway.Utilities.SerializeFromXML<Constants>(contents);
			}
			
		}
		
		//saves data
		public void save(){
			//SaveFile saveFile = generateSave();
			
			constants.compatibilityMode = core.compatibilityMode;
			
			TextWriter file = MyAPIGateway.Utilities.WriteFileInWorldStorage(fileName, typeof(Constants));
			file.Write(MyAPIGateway.Utilities.SerializeToXML(constants));
			file.Close();
			
		}
		
	}
	
}