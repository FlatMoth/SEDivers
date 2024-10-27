using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
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


namespace SED {

	[MySessionComponentDescriptor(MyUpdateOrder.Simulation, 999)]
	public class Core : MySessionComponentBase {
		
		//code components
		public Grid grid;
		public NetComm networker;
		public constantManager cm;
		public ClientConstantManager ccm;
		public SpawnEvent spawner;
		public UI ui;
		
		//data lists for framework
		public HashSet<string> aggroShips = new HashSet<string>();
		public HashSet<string> friendlies = new HashSet<string>();
		public HashSet<IMyBeacon> beacons = new HashSet<IMyBeacon>();
		private Dictionary<IMyFunctionalBlock, int> disabledTurrets = new Dictionary<IMyFunctionalBlock, int>();
		public Dictionary<string, SEDFaction> factions = new Dictionary<string, SEDFaction>();
		public Dictionary<MyCubeGrid, Tile> targets = new Dictionary<MyCubeGrid, Tile>();
		
		//savefile
		private string fileName = "galacticWar.cfg";
		
		//amount paid for order completion
		public long compensation = 25000000;
		
		//counter
		private int counter = 0;
		private int advanceTime = 1200;
		
		//misc game constants
		public bool compatibilityMode = false;
		public bool allowDynamicMap = true;
		private bool forceAlliesFriendly = false;
		private int minPlayers = 1;
		
		//mod randomizer
		public MyRandom rand = new MyRandom(1216);
		
		private bool showHint = false;
		
		public bool sentFindMe = false;
		
		private WcApi wcApi;
		
		public bool isDediServer = false;
		
		private List<List<string>> tmpPlanetTiles;
		
		
		//init
		public override void Init(MyObjectBuilder_SessionComponent sessionComponent){
			isDediServer = MyAPIGateway.Utilities.IsDedicated;
			wcApi = new WcApi();
			
			
			
			networker = new NetComm(this);
			cm = new constantManager(this);
			
			compatibilityMode = cm.constants.compatibilityMode;
			//if(compatibilityMode){
				wcApi.Load();
			//}
			
			ccm = new ClientConstantManager(this);
			spawner = new SpawnEvent(this);
			
			advanceTime = cm.constants.advanceTime;
			compensation = cm.constants.compensation;
			minPlayers = cm.constants.minPlayers;
			allowDynamicMap = cm.constants.allowDynamicMap;
			forceAlliesFriendly = cm.constants.forceAlliesFriendly;
			grid = new Grid(cm.constants.tileSize, cm.constants.gridSize, this);
			ui = new UI(this);
			MyAPIGateway.Utilities.MessageEntered += onChat;
			loadSave();
		}
		
		protected override void UnloadData(){
			MyAPIGateway.Utilities.MessageEntered -= onChat;
			grid.close();
			spawner.stop();
			networker.close();
			
			if(compatibilityMode){
				wcApi.Unload();
			}
		}
		
		//updates jammers, targets, random, etc
		private void jammerUpdate(){
			if(MyAPIGateway.Multiplayer.IsServer){
				
				updateTargets();
				updateBeacons();
				
				//MyAPIGateway.Utilities.ShowMessage("SEDivers",  "" + beacons.Count);
				//MyAPIGateway.Utilities.ShowMessage("SEDivers",  "" + disabledTurrets.Count);
							
				int throwaway = rand.Next(0, 1000);	
			}
		}
		
		private void enableWC(){
			compatibilityMode = true;
			//wcApi.Load();
		}
		
		private void disableWC(){
			compatibilityMode = false;
			//wcApi.Unload();
		}
		
		public void setFactionsNeutral(){
			
			if(!forceAlliesFriendly){
				return;
			}
			
			List<IMyPlayer> players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players, null);
			
			foreach(IMyPlayer player in players){
				
				foreach(string tag in friendlies){
					IMyFaction faction = MyAPIGateway.Session.Factions.TryGetFactionByTag(tag);
					
					if(faction != null){
						
						MyAPIGateway.Session.Factions.SetReputationBetweenPlayerAndFaction(player.PlayerID, faction.FactionId, 3000);
						
					}
				}
			}
			
		}
		
		//advances NPCs
		private void NPCFacUpdate(){
			
			//MyAPIGateway.Utilities.ShowMessage("SEDivers", "Advancing Factions");
			
			int loopCount = 0;
			int loopGoal = 0;
			try{
				loopGoal = rand.Next(0, factions.Count);
			}
			catch(Exception ex){
				
			}
			
			foreach(KeyValuePair<string, SEDFaction> entry in factions){
				
				//entry.Value.advance();
				
				if(loopCount >= loopGoal){
					entry.Value.advance();
					return;
				}
				loopCount++;
			}
		}
		
		//main thread, runs almost everything as thread
		public override void Simulate(){
			
			if(counter >= advanceTime){
				
				counter = 0;
				
				//MyAPIGateway.Utilities.ShowMessage("SEDivers",  "count " + counter);
				if(MyAPIGateway.Session.IsServer && allowDynamicMap){
					
					//MyAPIGateway.Utilities.ShowMessage("SEDivers", "PreAdvancing Factions");
					if(!isDediServer){
						MyAPIGateway.Parallel.StartBackground(NPCFacUpdate);
					}
					else{
						NPCFacUpdate();
					}
				}
			}
			
			if(counter % 900 == 0){
				
				if(MyAPIGateway.Session.IsServer){
					
					if(isDediServer){
						grid.update();
						jammerUpdate();
					}
					else{
						MyAPIGateway.Parallel.StartBackground(grid.update);
						MyAPIGateway.Parallel.StartBackground(jammerUpdate);
					}
				}
				else{
					if(!showHint){
						networker.requestMapSize();
					}
				}
				
				ui.checkHideObjectiveBox();
				
				if(!showHint){
					grid.open();
					if(tmpPlanetTiles != null){
						grid.unpackPlanetSave(tmpPlanetTiles);
					}
					
					
					if(isDediServer){
						grid.updatePlanets();
					}
					else{
						MyAPIGateway.Parallel.StartBackground(grid.updatePlanets);
					}
					
				}
			}
			if(counter % 30 == 1 && MyAPIGateway.Session.IsServer){
				spawner.clearQueue();
				spawner.activateAntennas();
			}
			if(counter % 900 == 180){
				if(!showHint){
					showHint = true;
					MyAPIGateway.Utilities.ShowMessage("SEDivers",  "Type /SEDHelp for SEDivers Commands");
					
					if(MyAPIGateway.Multiplayer.IsServer){
						setFactionsNeutral();
						networker.sendMapUpdate();
					}
					else{
						networker.sendGPSRequest();
					}
					
					if(!ccm.quietMode){
						ui.drawCompass();
					}
				}
			}
			
			
			
			if(MyAPIGateway.Players.Count >= minPlayers){
				counter++;
			}
			//MyAPIGateway.Utilities.ShowMessage("SEDivers", counter + "");
			
			if(MyAPIGateway.Multiplayer.IsServer){
				try{
					
					if(isDediServer){
						updateTurrets();
					}
					else{
						MyAPIGateway.Parallel.StartBackground(updateTurrets);
					}
				}
				catch(Exception e){
					
				}
			}
			else {
				
				
				try{
					if(isDediServer){
						ui.tick();
					}
					else{
						MyAPIGateway.Parallel.StartBackground(ui.tick);
					}
				}
				catch(Exception e){
				
				}
			}
			
		}
		
		
		
		
		//savefile stuff
		private void loadSave(){
			
			if(MyAPIGateway.Utilities.FileExistsInWorldStorage(fileName, typeof(SaveFile))){
				
				TextReader file = MyAPIGateway.Utilities.ReadFileInWorldStorage(fileName, typeof(SaveFile));
                string contents = file.ReadToEnd();
                file.Close();
				
				SaveFile saveFile = MyAPIGateway.Utilities.SerializeFromXML<SaveFile>(contents);
				
				loadSaveData(saveFile);
			}
			
		}
		
		//loads save from raw data
		public void loadSaveData(SaveFile saveFile){
			grid.center = saveFile.center;
			grid.extractOwnerSet(saveFile.tiles);
			tmpPlanetTiles = saveFile.planets;
			
			ui.updateObjective(saveFile.objTitle, saveFile.contents);
			
		}
		
		//used to set up worldfile for admins
		public void performFastSetup(Vector3 v){
			
			if(true){	
				//Set world center
				SetCenterPayload scp = new SetCenterPayload();
				scp.center = v;
				networker.sendPayloadServer(scp);
				
				//setup home tiles
				foreach(KeyValuePair<string, SEDFaction> entry in factions){
					grid.setRandomTile(entry.Key);
				}
				
				//populate Tiles
				if(isDediServer){
					autoPop();
				}
				else{
					MyAPIGateway.Parallel.StartBackground(autoPop);
				}
				
				//set starting tile safe
				Tile tile = grid.getTile(v);
				tile.liberate();
				
				//logging
				MyAPIGateway.Utilities.ShowMessage("SEDivers", "World Setup Complete");
				if(MyAPIGateway.Multiplayer.MultiplayerActive){
					MyAPIGateway.Utilities.SendMessage("World Setup Complete");
				}
				//networker.sendMapUpdate();
				
				//save data to file
				SaveData();
				
			}
		}
		
		//save savefile
		public override void SaveData(){
			
			SaveFile saveFile = generateSave();
			
			TextWriter file = MyAPIGateway.Utilities.WriteFileInWorldStorage(fileName, typeof(SaveFile));
			file.Write(MyAPIGateway.Utilities.SerializeToXML(saveFile));
			file.Close();
			cm.save();
			ccm.save();
			
		}
		
		//what it says on the tin
		public SaveFile generateSave(){
			return new SaveFile(grid.center, grid.generateOwnerSet(), ui.objTitle, ui.objInstruct, grid.generatePlanetSave());
		}
		
		//checks for commands
		private void onChat(string txt, ref bool share){
			
			
			if(txt.ToLower().StartsWith("/map")){
				
				share = false;
				
				//shows map in chat
				MapPayload mp = new MapPayload();
				networker.sendPayloadServer(mp);
				
			}
			if(txt.ToLower().StartsWith("/setcenter") && (MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Admin || MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Owner)){
				
				share = false;
				
				//sets world center if admin
				SetCenterPayload scp = new SetCenterPayload();
				scp.center = MyAPIGateway.Session.Player.GetPosition();
				networker.sendPayloadServer(scp);
			}
			if(txt.ToLower().StartsWith("/neworder") && (MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Admin || MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Owner)){
				
				share = false;
				
				//creates new objective if admin
				try{
					txt = txt.Substring(10);
					string[] segments = txt.Split(':');
					
					string title;
					string desc;
					
					if(txt.StartsWith(":")){
						title = "";
						desc = segments[0];
					}
					else{
						title = segments[0];
						desc = segments[1];
					}
					
					networker.sendNewObjective(title, desc);
				}
				catch (Exception ex){
					
				}
			}
			if(txt.ToLower().StartsWith("/clearobjective") && (MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Admin || MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Owner)){
				
				share = false;
				
				try{
					networker.sendNewObjective("", "");
					
					if(txt.StartsWith("/clearobjective done") || txt.StartsWith("/ClearObjective done")){
						Payload objCompPay = new Payload();
						objCompPay.type = PayloadType.objectiveComplete;
						
						networker.sendPayloadServer(objCompPay);
						
					}
					
				}
				catch (Exception ex){
					
				}
			}
			if(txt.ToLower().StartsWith("/setstatic") && (MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Admin || MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Owner)){
				
				share = false;
				
				//sets tile owner if admin
				string[] txtLst = txt.Split(' ');
				SetTilePayload stp = new SetTilePayload();
				stp.coords = MyAPIGateway.Session.Player.GetPosition();
				if(txtLst.Length > 0){
					stp.contents = txtLst[1];
				}
				networker.sendPayloadServer(stp);
			}
			if(txt.ToLower().StartsWith("/sedhelp")){
				
				share = false;
				
				//help command
				MyAPIGateway.Utilities.ShowMessage("SEDivers",  "\nSEDivers Command List\n============================\n\n/Map - Pulls up Galactic War Map\n\n/SetCenter - (Admin only) Sets map center for Galactic War\n\n/Planets - returns a list of all planets and who owns them\n\n/QuietMode - disables sound, UI, and gps notifications. This setting only affects you and will travel with you to other worlds\n\n/SetStatic - (Admin only) Sets current tile to static ownership\n\n/treasonmode - hides all new orders\n\n/SEDpopulate - (Admin only) autopopulates tiles\n\n/SEDlocation - tells the player what sector they are in\n\n/fastSetup - (Admin only) automatically configures SEDivers for the world\n\n/NewOrder [title]:[instructions] - (Admin only) creates a custom order for players. Fill in the arguments to specify your order name and instructions\n\n/ClearObjective - (Admin only) recalls an order in play. Adding the word \"done\" on the end will complete the order and issue rewards\n\n/toggleWC - (Admin only) toggle weaponcore compatibility. Having WC compatibility ON may cause issues with conveyor sorters");
			}
			if(txt.ToLower().StartsWith("/sedpopulate") && (MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Admin || MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Owner)){
				
				share = false;
				
				//autopopulates grid. useful for custom setups
				if(MyAPIGateway.Multiplayer.IsServer){
					if(isDediServer){
						autoPop();
					}
					else{
						MyAPIGateway.Parallel.StartBackground(autoPop);
					}
					//MyAPIGateway.Utilities.ShowMessage("SEDivers", "World Tiles Populated");
				}
				else{
					networker.sendPopulate();
				}
			}
			if(txt.ToLower().StartsWith("/togglewc") && (MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Admin || MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Owner)){
				share = false;
				
				networker.toggleWC();
			}
			if(txt.ToLower().StartsWith("/fastsetup") && (MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Admin || MyAPIGateway.Session.Player.PromoteLevel == MyPromoteLevel.Owner)){
				
				share = false;
				
				//runs fastSetup on world, sets center to 0 if dedi server
				if(MyAPIGateway.Multiplayer.IsServer){
					if(MyAPIGateway.Utilities.IsDedicated){
						MyAPIGateway.Utilities.ShowMessage("SEDivers", "No Server Host! Using Origin as Center");
						performFastSetup(new Vector3(0,0,0));
					}
					else{
						performFastSetup(MyAPIGateway.Session.Player.GetPosition());
					}
					
				}
				else{
					networker.sendFastSetup();
				}
			}
			if(txt.ToLower().StartsWith("/sedlocation")){
				
				share = false;
				
				//gets player location
				if(MyAPIGateway.Multiplayer.IsServer){
					Tile t = grid.getTile(MyAPIGateway.Session.Player.GetPosition());
					
					if(t == null){
						MyAPIGateway.Utilities.ShowMessage("SEDivers", "Outside SEDivers Map Area (check your warConstats.cfg?)");
					}
					else{
						MyAPIGateway.Utilities.ShowMessage("SEDivers", t.name + "");
					}
				}
				else{
					sentFindMe = true;
					networker.sendFindMe();
				}
				
			}
			if(txt.ToLower().StartsWith("/planets")){
				
				share = false;
				
				//gets planet list
				networker.sendGetPlanets();
				
			}
			if(txt.ToLower().StartsWith("/quietmode")){
				
				share = false;
				
				if(ccm.quietMode){
					ccm.quietMode = false;
					MyAPIGateway.Utilities.ShowMessage("SEDivers", "Quiet Mode has been toggled OFF");
				}
				else{
					ccm.quietMode = true;
					MyAPIGateway.Utilities.ShowMessage("SEDivers", "Quiet Mode has been toggled ON");
				}
				
			}
			if(txt.ToLower().StartsWith("/treasonmode")){
				
				share = false;
				
				if(ccm.treasonMode){
					ccm.treasonMode = false;
					MyAPIGateway.Utilities.ShowMessage("SEDivers", "Treason Mode has been toggled OFF");
				}
				else{
					ccm.treasonMode = true;
					MyAPIGateway.Utilities.ShowMessage("SEDivers", "Treason Mode has been toggled ON");
				}
				
			}
			
			
			
		}
		
		//autofills grid. useful for custom setup
		public void autoPop(){
			int i = 0;
			while(i < 40){
				foreach(KeyValuePair<string, SEDFaction> entry in factions){
					entry.Value.advance();
				}
				i++;
			}
		}
		
		//disables turrets in range of jammers
		private void updateTurrets(){
			
			foreach(KeyValuePair<IMyFunctionalBlock, int> entry in disabledTurrets){

				entry.Key.Enabled = false;
			}
			
		}
		
		//finds turrets to add to disable queue
		private void updateBeacons(){
			
			disabledTurrets = new Dictionary<IMyFunctionalBlock, int>();
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
					
			MyAPIGateway.Entities.GetEntities(entities, null);
			
			
			foreach(IMyEntity e in entities){
				if(e is MyCubeGrid){
					MyCubeGrid igrid = e as MyCubeGrid;
					
					long owner = -1;
					
					if(igrid.BigOwners.Count >= 1){
						owner = igrid.BigOwners[0];
					}
					
					if(inBeaconRange(e.GetPosition(), owner)){
						
						foreach(IMySlimBlock sb in igrid.CubeBlocks){
							if(sb.FatBlock is IMyLargeTurretBase || sb.FatBlock is IMyTurretControlBlock || (sb.FatBlock is IMyConveyorSorter && compatibilityMode)){
								IMyFunctionalBlock fb = sb.FatBlock as IMyFunctionalBlock;
								if(fb.Enabled){
									
									fb.Enabled = false;
								}
								
								if(fb == null){
										
								}
								else if(!disabledTurrets.ContainsKey(fb)){
									disabledTurrets.Add(fb, 1000);
								}
								else{
									try{
										disabledTurrets[fb] = 1000;
									}
									catch(Exception exc){
											
									}
								}
							}
						}
					}
				}
			}
		}
		
		//checks if point in range of jammer as well as IFF check
		private bool inBeaconRange(Vector3 v, long owner){
			bool result = false;
			
			foreach(IMyBeacon b in beacons){
				int distance = (int)Vector3.Distance(v, b.GetPosition());
				
				bool isNPC = true;
				
				/*IMyPlayer f = Helpers.tryGetPlayerById(b.OwnerId);
				
				if(f != null){
					if(f.IsBot){
						isNPC = true;
					}
				}*/
				
				if(distance <= b.Radius && owner != b.OwnerId && isNPC){
					return true;
				}
			}
			
			return false;
		}
		
		
		//finds jammers and checks for objectives completed
		private void updateTargets(){
			//MyAPIGateway.Utilities.ShowMessage("SEDivers", "SCANNING");
			
			HashSet<MyCubeGrid> removeLst = new HashSet<MyCubeGrid>();
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
					
			MyAPIGateway.Entities.GetEntities(entities, null);
			
			beacons = new HashSet<IMyBeacon>();
					
			//looks for grids with beacons and for objectives
			foreach(IMyEntity e in entities){
				
				//IMyCubeGrid ig = e as IMyCubeGrid;
				if(e is MyCubeGrid){
					MyCubeGrid g = e as MyCubeGrid;
					
					//checks if ship is objective
					if(aggroShips.Contains(e.DisplayName)){
					
						
						Tile gt = grid.getTile(e.GetPosition());
						
						if(g.BigOwners.Count >= 1){
							IMyFaction gridFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(g.BigOwners[0]);
						
							if(gridFaction != null){
								if(gt != null && !targets.ContainsKey(g) && gridFaction.Tag == gt.owner){
									targets.Add(g, gt);
								}
							}
						}
					}
					
					//checks for jammer beacon, if so adds to beacon list
					foreach(IMySlimBlock b in g.CubeBlocks){
						if(b.FatBlock is IMyBeacon){
							IMyBeacon beacon = b.FatBlock as IMyBeacon;
					
							if(beacon.HudText == "COMMS JAMMER" || beacon.HudText == "WEAPONS JAMMER"){
								beacons.Add(beacon);
							}
						}
						//loop now doubles to check for friendly turrets with "target neutrals" on. Because for some unfathomable reason neutrals and friends are the same in WC.
						//if((b.FatBlock is IMyConveyorSorter || b.FatBlock is IMyLargeTurretBase) /*&& OWNER CHECK HERE*/){
							//WC disable target neutral
							
						//}
					}
				}
			}
			
			//MyAPIGateway.Utilities.ShowMessage("SEDivers", targets.Count + "");
			//MyAPIGateway.Utilities.ShowMessage("SEDivers", aggroShips.Count + "");
			
			//updates found objectives (aggroships list)
			foreach(KeyValuePair<MyCubeGrid, Tile> entry in targets){
				
				if(entry.Key == null){
					
				}
				else{
					//MyAPIGateway.Utilities.ShowMessage("SEDivers", entry.Key.DisplayName);
					
					//objective death check
					bool dead = true;
					
					foreach(IMySlimBlock block in entry.Key.CubeBlocks){
						IMyCubeBlock fatBlock = block.FatBlock;
						if(fatBlock is IMyPowerProducer){
							dead = false;
						}
					}
					
					//remova/scoring of dead objectives
					if(dead){
						//MyAPIGateway.Utilities.ShowMessage("SEDivers", "DEAD SHIP");
						int score = 0;
						
						if(entry.Key.IsStatic){
							score += 5;
							if(entry.Key.BlocksCount >= 6000){
								score += 5;
							}
						}
						else{
							if(entry.Key.BlocksCount >= 6000){
								score += 5;
							}
							else {
								score += 2;
							}
						}
					
						if(entry.Value != null){
							if(entry.Key.BigOwners.Count >= 1){
							
								IMyFaction gridFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(entry.Key.BigOwners[0]);
								if(true/*entry.Value.owner == gridFaction.Tag || (entry.Value.owner == "PLAYER" && friendlies.Contains(gridFaction.Tag))*/){
									entry.Value.capProgress -= score;
						
									if(MyAPIGateway.Multiplayer.IsServer){
										networker.sendMissionComplete(entry.Value.owner, entry.Value.name);
										removeLst.Add(entry.Key);
										//spawner.removeEntity(entry.Key);
									}
								}
							}
						}
					
					}
					
				}
				
			}
			
			targets = new Dictionary<MyCubeGrid, Tile>();
			
			//removal queue
			foreach(MyCubeGrid grid in removeLst){
				//targets.Remove(grid);
				spawner.removeEntity(grid);
				
			}
		}
	
	}
		
		
}