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
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using SpaceEngineers.Game.ModAPI;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;


namespace SED {
	
	public class UI {
		
		//main
		public Core core;
		
		//gps info
		public Dictionary<Vector3, string> coords = new Dictionary<Vector3, string>();
		private HashSet<int> gpsHashes = new HashSet<int>();
		
		//border draw info
		private int radius; //tile radius (even though its a square, don't question it)
		private int diam; //tile width
		private MyStringId shield_mat = MyStringId.GetOrCompute("SafeZoneShield_Material");
		private Color ZoneColor = new Color(50, 50, 0, 1);
		
		//obj. data
		public string objTitle;
		public string objInstruct;
		
		//(old) objective box
		//private IMyHudObjectiveLine orderBox = MyAPIGateway.Utilities.GetObjectiveLine();
		
		//second failed objective box
		//private int objHash;
		
		//newest objective box
		private bool orderComplete = false;
		private bool firstRun = true;
		
		//initializer
		public UI(Core c){
			core = c;
			radius = c.grid.tileSize/2;
			diam = c.grid.tileSize;
			

		}
		
		public void shareObjective(){
			core.networker.sendNewObjective(objTitle, objInstruct);
		}
		
		//updates major orders
		public void updateObjective(string title, string desc){
			
			//check for same objective
			if(desc == objInstruct){
				return;
			}
			
			objTitle = title;
			objInstruct = desc;
			
			if(title == "" || title == null){
				try{
					clearObjective();
					if(firstRun){
						MyVisualScriptLogicProvider.SetQuestlog(false, "");
					}
				}
				catch(Exception e){
					
				}
			}
			else{
				showObjective();
			}
			
			firstRun = false;
		}
		
		//resets orders
		private void clearObjective(){
			//old code
			//orderBox.Hide();
			
			MyVisualScriptLogicProvider.SetQuestlogDetailCompleted(0, true);
			orderComplete = true;
		}
		
		public void checkHideObjectiveBox(){
			if(orderComplete || objTitle == "" || objTitle == null){
				MyVisualScriptLogicProvider.SetQuestlog(false, "");
				orderComplete = false;
			}
		}
		
		//shows orders if not already visible
		private void showObjective(){
			MyVisualScriptLogicProvider.RemoveQuestlogDetails();
			
			
			//old code
			//orderBox.Title = "New Orders: " + objTitle;
			//orderBox.Objectives = new List<string>();
			//orderBox.Objectives.Add(objInstruct);
			//orderBox.Show();
			
			if(core.ccm.treasonMode){
				return;
			}
			
			MyVisualScriptLogicProvider.SetQuestlog(true, "New Orders: " + objTitle);
			//MyVisualScriptLogicProvider.AddQuestlogObjectiveLocal("New Orders: " + objTitle, true, true, MyAPIGateway.Session.Player.PlayerID);
			MyVisualScriptLogicProvider.AddQuestlogDetail("Directions:\n-----------------\n" + objInstruct, true, true);
			
			if(!core.ccm.quietMode){
				core.networker.playSoundEffect("NewOrder");
			}
		}
		
		//adds coords for NESW directions
		public void drawCompass(){
			IMyGps n = MyAPIGateway.Session.GPS.Create("Galactic East", "[Auto-generated compass GPS]", new Vector3(0,0,100000000000000), true, false);
			IMyGps s = MyAPIGateway.Session.GPS.Create("Galactic West", "[Auto-generated compass GPS]", new Vector3(0,0,-100000000000000), true, false);
			IMyGps e = MyAPIGateway.Session.GPS.Create("Galactic North", "[Auto-generated compass GPS]", new Vector3(100000000000000,0,0), true, false);
			IMyGps w = MyAPIGateway.Session.GPS.Create("Galactic South", "[Auto-generated compass GPS]", new Vector3(-100000000000000,0,0), true, false);
			
			MyAPIGateway.Session.GPS.AddLocalGps(n);
			MyAPIGateway.Session.GPS.AddLocalGps(s);
			MyAPIGateway.Session.GPS.AddLocalGps(e);
			MyAPIGateway.Session.GPS.AddLocalGps(w);
		}
		
		//updates list of hostile border sector GPSs. useful for helping players find ways to help
		public void updateUI(Dictionary<Vector3, string> lst){
			//MyAPIGateway.Utilities.ShowMessage("SEDivers",  "SENT UPDATE");
			foreach(int hash in gpsHashes){
				MyAPIGateway.Session.GPS.RemoveLocalGps(hash);
			}
			gpsHashes = new HashSet<int>();
			
			coords = lst;
			
			if(objTitle != "" && objTitle != null && !core.ccm.treasonMode){
				MyVisualScriptLogicProvider.SetQuestlog(true, "New Orders: " + objTitle);
				//MyVisualScriptLogicProvider.AddQuestlogObjectiveLocal("New Orders: " + objTitle, true, true, MyAPIGateway.Session.Player.PlayerID);
				MyVisualScriptLogicProvider.AddQuestlogDetail("Directions:\n-----------------\n" + objInstruct, false, false);
			}
			
			//sectors are saved in hashes to they can be removed and added from gps list easily 
			foreach(KeyValuePair<Vector3, string> v in coords){
				if(!core.ccm.quietMode){
					IMyGps gps = MyAPIGateway.Session.GPS.Create("Priority Sector: " + v.Value, "[Auto-generated GPS for bordering enemy tiles]", v.Key, true, false);
					gpsHashes.Add(gps.Hash);
					MyAPIGateway.Session.GPS.AddLocalGps(gps);
				}
			}
			
		}
		
		//draws sector borders
		public void tick(){
			foreach(KeyValuePair<Vector3, string> entry in coords){
				
				if(MyAPIGateway.Session.IsServer){
					return;
				}
				
				//calculate current sector to draw around
				Vector3 Playerpos = MyAPIGateway.Session.Player.GetPosition();
				
				Vector3 location = entry.Key;
				location.Y = Playerpos.Y;
				
				int xDist = (int)Math.Abs(location.X - Playerpos.X);
				int zDist = (int)Math.Abs(location.Z - Playerpos.Z);
				
				//((xDist >= radius-5000 && xDist <= radius) || (zDist >= radius-5000 && zDist <= radius)) && 
				
				//checks for if you are close to sector border(s)
				if(Vector3.Distance(location, Playerpos) <= radius * 1.42 && Vector3.Distance(location, Playerpos) >= radius * 0.95 && !MyVisualScriptLogicProvider.IsPlanetNearby(Playerpos)){
					
					//draw border
					MatrixD worldMatrix = MatrixD.CreateBillboard(location, location, location, null);
					BoundingBoxD box = new BoundingBoxD(new Vector3(0-radius,0-radius,0-radius), new Vector3(radius, radius, radius));
				
					//MySimpleObjectDraw.DrawTransparentSphere(ref worldMatrix, radius, ref ZoneColor, MySimpleObjectRasterizer.Solid, 35, shield_mat, null,
                                    //-1, -1, null, BlendTypeEnum.PostPP, 1);
					MySimpleObjectDraw.DrawTransparentBox(ref worldMatrix, ref box, ref ZoneColor, MySimpleObjectRasterizer.Solid, 35, -1, shield_mat, null, false,
                                    -1, BlendTypeEnum.PostPP, 1, null);
					
				}
				
				
			}
		}
		
		
	}
	
}