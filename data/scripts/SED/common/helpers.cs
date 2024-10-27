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


namespace SED {
	
	
	public static class Helpers {
		
		
		
		public static IMyPlayer tryGetPlayerById(long id){
			List<IMyPlayer> players = new List<IMyPlayer>();
			
			MyAPIGateway.Players.GetPlayers(players, null);
			
			foreach(IMyPlayer p in players){
				if(p.PlayerID == id){
					return p;
				}
			}
			return null;
		}
		
		public static IMyEntity tryGetEntityById(long id){
			HashSet<IMyEntity> ents = new HashSet<IMyEntity>();
			
			MyAPIGateway.Entities.GetEntities(ents, null);
			
			foreach(IMyEntity e in ents){
				if(e.EntityId == id){
					return e;
				}
			}
			return null;
		}
		
		public static IMyGps generateGPS(string input){
			
			List<string> stringParts = input.Split(':').ToList();
			
			string name = "[ZONE]";
			float x = 0f;
			float y = 0f;
			float z = 0f;
			
			try{
				
				name = stringParts[1];
				
				x = float.Parse(stringParts[2]);
				y = float.Parse(stringParts[3]);
				z = float.Parse(stringParts[4]);
				
			}
			catch(Exception e){}
			
			IMyGps result = MyAPIGateway.Session.GPS.Create(name, "Auto-Generated Cap Zone", new Vector3(x, y, z), true, false);
			
			return result;
			
		}
		
	}
	
}