using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Audio;
using VRage.Game.Components;
using VRage.Game.ModAPI;
//using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;
using VRageMath;
using VRage.Library.Utils;
using VRage.ModAPI;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Planet;
using System.Numerics;
using SpaceEngineers.Game.ModAPI;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI.Ingame.Utilities;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.Entities.Blocks;
using System.IO;
using System.Xml.Serialization;
using ProtoBuf;

namespace SED {
	
	public class NetComm {
		
		//networkin info
		private ushort port = 4096;
		private ushort modPort = 333;
		
		private Core core;
		
		//user status checks
		private bool isServer = MyAPIGateway.Multiplayer.IsServer || !MyAPIGateway.Multiplayer.MultiplayerActive;
		private bool isClient = !MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Multiplayer.MultiplayerActive;
		
		//initializer
		public NetComm(Core process){
			core = process;
			MyAPIGateway.Multiplayer.RegisterMessageHandler(port, OnMsgNetwork);
			MyAPIGateway.Utilities.RegisterMessageHandler(modPort, OnModMsg);
		}
		
		//closure
		public void close(){
			MyAPIGateway.Multiplayer.UnregisterMessageHandler(port, OnMsgNetwork);
			MyAPIGateway.Utilities.UnregisterMessageHandler(modPort, OnModMsg);
		}
		
		//used for communicating with SEDivers Mods
		public void OnModMsg(object o){
			
			//load config, try catch for in case user adds duplicate entry
			try{
				ModConfig mc = MyAPIGateway.Utilities.SerializeFromBinary<ModConfig>(o as byte[]);
				
				foreach(string s in mc.factions){
					if(!core.factions.ContainsKey(s)){
						core.factions.Add(s, new SEDFaction(s, core));
						MyAPIGateway.Utilities.ShowMessage("SEDivers", "Loading Faction: " + s);
					}
				}
				
				foreach(string entry in mc.aggroShips){
					if(!core.aggroShips.Contains(entry)){
						core.aggroShips.Add(entry);
					}
				}
				
				foreach(string entry in mc.friendlyFactions){
					if(!core.friendlies.Contains(entry)){
						core.friendlies.Add(entry);
					}
				}
			}
			catch (Exception e){
				
			}
		}
		
		//used for ingame networking
		private void OnMsgNetwork(byte[] byteCode){
			Payload p = MyAPIGateway.Utilities.SerializeFromBinary<Payload>(byteCode);
			
			//checks paylaod type, then runs code based on type
			switch(p.type){
				case PayloadType.setCenter:
					//set world center
					
					int offset = core.grid.offset;
					
					SetCenterPayload scp = MyAPIGateway.Utilities.SerializeFromBinary<SetCenterPayload>(byteCode);
					
					Vector3 center = scp.center;
					center.X -= offset;
					center.Z -= offset;
					core.grid.center = center;
					
					if(core.isDediServer){
						core.grid.updatePlanets();
					}
					else{
						MyAPIGateway.Parallel.StartBackground(core.grid.updatePlanets);
					}
					
					sendCenter();
					MyVisualScriptLogicProvider.SendChatMessage("Map center set to: " + center.X + offset + ", " + center.Z + offset, "SEDivers");
					/*if(MyAPIGateway.Session.IsServer){
						MyAPIGateway.Utilities.ShowMessage("SEDivers", "Map center set to: " + center.X + offset + ", " + center.Z + offset);
					}*/
					sendMapUpdate();
					break;
				case PayloadType.setTile:
					//set tile ownership
				
					SetTilePayload stp = MyAPIGateway.Utilities.SerializeFromBinary<SetTilePayload>(byteCode);
					
					Tile tile = core.grid.getTile(stp.coords);
					if(tile == null){
						break;
					}
					
					tile.setStatic(stp.contents);
					MyVisualScriptLogicProvider.SendChatMessage("Tile owner set to: " + stp.contents, "SEDivers");
					/*if(MyAPIGateway.Session.IsServer){
						MyAPIGateway.Utilities.ShowMessage("SEDivers", "Tile owner set to: " + stp.contents);
					}*/
					sendMapUpdate();
					break;
				case PayloadType.requestMap:
					//draw world map for all
					core.grid.drawMap();
					break;
				case PayloadType.missionComplete:
					//sends mission completion status to users
					MissionCompletePayload mcp = MyAPIGateway.Utilities.SerializeFromBinary<MissionCompletePayload>(byteCode);
					
					processMissionComplete(mcp);
					break;
				case PayloadType.tileCap:
					//seds tile capture notif to users
					TileCapPayload mcpb = MyAPIGateway.Utilities.SerializeFromBinary<TileCapPayload>(byteCode);
					
					processTileCap(mcpb);
					break;
				case PayloadType.gpsPayload:
					//client: loads priority sectors
					GpsPayload gp = MyAPIGateway.Utilities.SerializeFromBinary<GpsPayload>(byteCode);
					
					processMapUpdate(gp);
					break;
				case PayloadType.gpsPayloadRequest:
					//asks server for priority sectors
					sendMapUpdate();
					core.ui.shareObjective();
					core.setFactionsNeutral();
					break;
				case PayloadType.autoPopulate:
					//autopopulates grid
					
					if(core.isDediServer){
						core.autoPop();
					}
					else{
						MyAPIGateway.Parallel.StartBackground(core.autoPop);
					}
					
					
					MyVisualScriptLogicProvider.SendChatMessage("Populating World Tiles", "SEDivers");
					break;
				case PayloadType.getCurrentTile:
					//returns current user sector
					sendCenter();
					break;
				case PayloadType.fastSetup:
					//performs world fastSetup
					FastSetupPayload fsp = MyAPIGateway.Utilities.SerializeFromBinary<FastSetupPayload>(byteCode);
				
					core.performFastSetup(fsp.center);
					break;
				case PayloadType.getCenter:
					//get world center, uses it to find sectors
					GetCenterPayload gcp = MyAPIGateway.Utilities.SerializeFromBinary<GetCenterPayload>(byteCode);
					if(core.grid.center != gcp.center){
						core.grid.center = gcp.center;
						
						
						if(core.isDediServer){
							core.grid.updatePlanets();
						}
						else{
							MyAPIGateway.Parallel.StartBackground(core.grid.updatePlanets);
						}
					}
					
					if(core.sentFindMe){
						
						processFindMe();
						core.sentFindMe = false;
					}
					break;
				case PayloadType.newObjective:
					//updates objective, may cause crash on dedi server?
					
					NewObjectivePayload nop = MyAPIGateway.Utilities.SerializeFromBinary<NewObjectivePayload>(byteCode);
					
					try{
						core.ui.updateObjective(nop.title, nop.contents);
					}
					catch(Exception e){
						
					}
					
					break;
				case PayloadType.objectiveComplete:
					//rewards players for objective completion
					
					HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
					HashSet<long> playerIds = new HashSet<long>();
					
					MyAPIGateway.Entities.GetEntities(entities, null);
					
					/*foreach(IMyPlayer play in players){
						play.RequestChangeBalance(core.compensation);
					}*/
					
					
					foreach(IMyEntity ent in entities){
						if(ent is MyCubeGrid){
							MyCubeGrid tGrid = ent as MyCubeGrid;
							
							if(tGrid.BigOwners.Count > 0){
								if(!playerIds.Contains(tGrid.BigOwners[0])){
									MyAPIGateway.Players.RequestChangeBalance(tGrid.BigOwners[0], core.compensation);
									
									playerIds.Add(tGrid.BigOwners[0]);
								}
							}
						}
					}
					
					
					notifObjComp();

					break;
				case PayloadType.getPlanets:
					//returns list of planets and ownership
					core.grid.generatePlanetList();
					break;
				case PayloadType.requestMapSize:
					sendMapSize();
					break;
				case PayloadType.sendMapSize:
					setMapSizePayload smsp = MyAPIGateway.Utilities.SerializeFromBinary<setMapSizePayload>(byteCode);
					
					//core.grid.gridSize = smsp.gridSize;
					//core.grid.tileSize = smsp.tileSize;
					
					core.grid = new Grid(smsp.tileSize, smsp.gridSize, core);
					
					break;
				case PayloadType.toggleWC:
					
					if(core.compatibilityMode){
						core.compatibilityMode = false;
						MyVisualScriptLogicProvider.SendChatMessage("Weaponcore compatibility is toggled OFF", "SEDivers");
					}
					else{
						core.compatibilityMode = true;
						MyVisualScriptLogicProvider.SendChatMessage("Weaponcore compatibility is toggled ON", "SEDivers");
					}
					
					break;
			}
			
		}
		
		//sends notification to players of objective completion
		private void notifObjComp(){
			MissionCompletePayload mcp = new MissionCompletePayload();
			
			mcp.color = "Yellow";
			mcp.contents = "Shared Orders Complete!";
			
			sendPayloadClient(mcp);
			processMissionComplete(mcp);
		}
		
		//requests map dimensions
		public void requestMapSize(){
			Payload p = new Payload();
			
			p.type = PayloadType.requestMapSize;
			
			sendPayloadServer(p);
		}
		
		//sends map dimensions
		public void sendMapSize(){
			setMapSizePayload smsp = new setMapSizePayload();
			
			smsp.gridSize = core.grid.gridSize;
			smsp.tileSize = core.grid.tileSize;
			
			sendPayloadClient(smsp);
		}
		
		public void toggleWC(){
			Payload p = new Payload();
			
			p.type = PayloadType.toggleWC;
			
			sendPayloadServer(p);
		}
		
		
		//sends update to all other users for new objective
		public void sendNewObjective(string title, string desc){
			
			NewObjectivePayload nop = new NewObjectivePayload();
			
			nop.title = title;
			nop.contents = desc;
			
			sendPayloadServer(nop);
			sendPayloadClient(nop);
			
		}
		
		public void sendGetPlanets(){
			Payload p = new Payload();
			
			p.type = PayloadType.getPlanets;
			
			sendPayloadServer(p);
		}
		
		//returns to client current sector
		public void processFindMe(){
			Tile t = core.grid.getTile(MyAPIGateway.Session.Player.GetPosition());
			
			
			if(t == null){
				MyAPIGateway.Utilities.ShowMessage("SEDivers", "Outside SEDivers Map Area!");
			}
			else{		
				MyAPIGateway.Utilities.ShowMessage("SEDivers", "You are in Sector: "+t.name);
			}
		}
		
		
		//runs client mission completion announcement logic
		public void processMissionComplete(MissionCompletePayload mcp){
			
			if(core.ccm.quietMode){
				return;
			}
			
			if(mcp.color == "Red"){
				MyAPIGateway.Utilities.ShowNotification(mcp.contents, 10000, "Red");
				playSoundEffect("ZoneLost");
			}
			else{
				MyAPIGateway.Utilities.ShowNotification("["+mcp.contents+"]", 10000, "White");
				playSoundEffect("ZoneCapped");
			}
		}
		
		//runs client tile capture announcement logic
		public void processTileCap(TileCapPayload mcp){
			
			if(core.ccm.quietMode){
				return;
			}
			
			if(mcp.color == "Red"){
				MyAPIGateway.Utilities.ShowNotification(mcp.contents, 10000, "Red");
				playSoundEffect("ZoneLost");
			}
			else{
				MyAPIGateway.Utilities.ShowNotification("["+mcp.contents+"]", 10000, "White");
				playSoundEffect("ZoneCapped");
			}
		}
		
		//updates priority sectors on client
		public void processMapUpdate(GpsPayload gp){
			try{
				core.ui.updateUI(gp.coords);
			}
			catch(Exception e){
				
			}
		}
		
		
		
		
		//sends payload to server
		public void sendPayloadServer(Payload p){
			MyAPIGateway.Multiplayer.SendMessageToServer(port, MyAPIGateway.Utilities.SerializeToBinary(p), true);
		}
		
		//sends payload to all clients
		public void sendPayloadClient(Payload p){
			MyAPIGateway.Multiplayer.SendMessageToOthers(port, MyAPIGateway.Utilities.SerializeToBinary(p), true);
		}
		
		//sends request for priority sector update
		public void sendGPSRequest(){
			Payload p = new Payload();
			
			p.type = PayloadType.gpsPayloadRequest;
			
			sendPayloadServer(p);
		}
		
		//sends grid population request
		public void sendPopulate(){
			Payload p = new Payload();
			
			p.type = PayloadType.autoPopulate;
			
			sendPayloadServer(p);
		}
		
		//sends fastSetup request
		public void sendFastSetup(){
			FastSetupPayload p = new FastSetupPayload();
			
			p.type = PayloadType.fastSetup;
			p.center = MyAPIGateway.Session.Player.GetPosition();
			
			sendPayloadServer(p);
		}
		
		//sends current location request
		public void sendFindMe(){
			Payload p = new Payload();
			
			p.type = PayloadType.getCurrentTile;
			
			sendPayloadServer(p);
		}
		
		//sends center change request
		public void sendCenter(){
			GetCenterPayload p = new GetCenterPayload();
			
			p.center = core.grid.center;
			
			sendPayloadClient(p);
		}
		
		//sends priority sector update to clients
		public void sendMapUpdate(){
			GpsPayload gp = new GpsPayload();
			
			gp.coords = core.grid.exportTiles();
			
			sendPayloadClient(gp);
			sendCenter();
			processMapUpdate(gp);
		}
		
		//sends mission completion update to clients
		public void sendMissionComplete(string tag, string name){
			
			MissionCompletePayload mcp = new MissionCompletePayload();
			
			if(tag == "PLAYER"){
				mcp.color = "Red";
				mcp.contents = "("+ name +") Allied Target Destroyed";
			}
			else {
				mcp.color = "Yellow";
				mcp.contents = "("+ name +") Mission Complete: Target Destroyed";
			}
			
			
			sendPayloadClient(mcp);
			processMissionComplete(mcp);
			
		}
		
		//sends tile capture update to clients
		public void sendTileCap(string tag, string name){
			
			TileCapPayload tcp = new TileCapPayload();
			
			if(tag == "PLAYER"){
				tcp.color = "Yellow";
				tcp.contents = "("+ name +") Sector Captured";
			}
			else {
				tcp.color = "Red";
				tcp.contents = "("+ name +") Sector Lost";
			}
			
			
			sendPayloadClient(tcp);
			processTileCap(tcp);
			
		}
		
		//plays sound effect. useful for mission/capture updates
		public void playSoundEffect(string s){
			MySoundPair sound = new MySoundPair(s);
			var player = MyAPIGateway.Session.Player;
			
			try{
				var soundEff = player.Character.Components.Get<MyCharacterSoundComponent>();
			
				soundEff.PlayActionSound(sound);
			}
			catch(Exception e){
				
			}
			
			//return;
		}
		
	}
	
	
	
	
}