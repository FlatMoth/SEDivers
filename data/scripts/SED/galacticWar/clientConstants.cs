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
	//playerConstants savefile
	public class ClientConstants {
		
		[ProtoMember(1)]
		public bool quietMode = false;
		
		[ProtoMember(2)]
		public bool treasonMode = false;
		
		public ClientConstants(){
			
		}
		
	}
	
	//manager for above saveFile
	public class ClientConstantManager {
		
		//filename
		private string fileName = "playerConfig.cfg";
		
		private Core core;
		
		//data
		public bool quietMode = false;
		public bool treasonMode = false;
		
		//saveFile
		public ClientConstants constants = new ClientConstants();
		
		//initializer
		public ClientConstantManager(Core process){
			core = process;
			load();
			update();
		}
		
		//fills in saveFile
		public void update(){
			quietMode = constants.quietMode;
			treasonMode = constants.treasonMode;
		}
		
		//savefile loader
		public void load(){
			
			if(MyAPIGateway.Utilities.FileExistsInLocalStorage(fileName, typeof(ClientConstants))){
				
				TextReader file = MyAPIGateway.Utilities.ReadFileInLocalStorage(fileName, typeof(ClientConstants));
                string contents = file.ReadToEnd();
                file.Close();
				
				constants = MyAPIGateway.Utilities.SerializeFromXML<ClientConstants>(contents);
			}
			
		}
		
		//saves data
		public void save(){
			
			constants.quietMode = quietMode;
			constants.treasonMode = treasonMode;
			
			//SaveFile saveFile = generateSave();
			
			TextWriter file = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(ClientConstants));
			file.Write(MyAPIGateway.Utilities.SerializeToXML(constants));
			file.Close();
			
		}
		
	}
	
}