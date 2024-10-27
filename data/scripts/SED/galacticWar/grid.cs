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
	
	public class Grid {
		
		//grid constants
		public int tileSize = 500000;
		public int gridSize = 15;
		public int offset = 3750;
		
		//admin-designated world center
		public Vector3 center = new Vector3(0,0,0);
		
		//main
		private Core core;
		
		//tiles
		public List<List<Tile>> cells = new List<List<Tile>>();
		public Dictionary<long, Tile> planetCells = new Dictionary<long, Tile>();
		
		//init
		public Grid(int tileSizet, int gridSizet, Core c){
			
			tileSize = tileSizet;
			gridSize = gridSizet;
			core = c;
			offset = gridSize * tileSize / 2;
			for(int i = 0; i<gridSize;i++){
				List<Tile> row = new List<Tile>();
				for(int j = 0; j<gridSize;j++){
					row.Add(new Tile(i,j,c, core.cm.constants.capturePoints));
				}
				cells.Add(row);
			}
		}
		
		//register events
		public void open(){
			MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
		}
		
		//unregisters events
		public void close(){
			MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
		}
		
		//update grid and all attached tiles
		public void update(){
			
			for(int i = 0; i<gridSize;i++){
				
				for(int j = 0; j<gridSize;j++){
					cells[i][j].update();
				}
			}
			
			foreach(KeyValuePair<long, Tile> entry in planetCells){
				entry.Value.update();
			}
		}
		
		//export tiles to share with other players
		public Dictionary<Vector3, string> exportTiles(){
			Dictionary<Vector3, string> badTiles = new Dictionary<Vector3, string>();
			
			//searches space tiles, if owned by npc and bordering player tile, mark to send
			for(int i = 0; i<gridSize;i++){
				for(int j = 0; j<gridSize;j++){
					Tile t = cells[i][j];
					
					if(core.factions.ContainsKey(t.owner)){
						bool addTo = false;
						
						if(i>0){
							if(cells[i-1][j].owner == "PLAYER"){
								addTo = true;
							}
						}
						if(i<gridSize-1){
							if(cells[i+1][j].owner == "PLAYER"){
								addTo = true;
							}
						}
						if(j>0){
							if(cells[i][j-1].owner == "PLAYER"){
								addTo = true;
							}
						}
						if(j<gridSize-1){
							if(cells[i][j+1].owner == "PLAYER"){
								addTo = true;
							}
						}
						
						if(addTo){
							badTiles.Add(getTilePos(t), t.name);
						}
					}
				}
			}
			
			//add all npc-owned planets
			foreach(KeyValuePair<long, Tile> entry in planetCells){
				if(entry.Value.owner != "PLAYER"){
					IMyEntity entTmp = MyAPIGateway.Entities.GetEntityById(entry.Key);
					
					badTiles.Add(entTmp.PositionComp.GetPosition(), entry.Value.name);
				}
			}
			
			//MyAPIGateway.Utilities.ShowMessage("SEDivers",  badTiles.Count + "");
			return badTiles;
		}
		
		
		//condenses world coords into grid coords
		public string shortenCoords(Vector3 v){
			v = v-center;
			string s = v.X/tileSize + ";" + v.Z/tileSize;
			return s;
		}
		
		//extends grid coords to world coords from gps string
		public Vector3 lengthenCoords(string s){
			string[] coordsStrings = s.Split(';');
			
			Vector3 coords = new Vector3(int.Parse(coordsStrings[0])*tileSize, 0 , int.Parse(coordsStrings[1])*tileSize);
			coords = coords + center;
			
			return coords;
		}
		
		//extends grid coords to world coords from x,y
		public Vector3 lengthenCoords(int x, int y){
			
			Vector3 coords = new Vector3(x*tileSize, 0 , y*tileSize);
			coords = coords + center;
			
			return coords;
		}
		
		//gets center of known tile
		public Vector3 getTilePos(Tile t){
			
			int x = t.x;
			int y = t.y;
			
			x=x*tileSize;
			y=y*tileSize;
			Vector3 result = new Vector3(x+tileSize/2, center.Y, y+tileSize/2);
			
			result = result + center;
			return result;
		}
		
		//get tile that a point is in
		public Tile getTile(Vector3 v){
			
			//search planets
			//
			//TODO: actually get nearest planet FULL NAME, not nearest planet name
			if(MyVisualScriptLogicProvider.IsPlanetNearby(v)){
				
				float distance = -1f;
				IMyEntity nearestPlanet = null;
				
				HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
				MyAPIGateway.Entities.GetEntities(entities, null);
				
				foreach(IMyEntity ent in entities){
					if(ent is MyPlanet){
						
						if(distance == -1f || Vector3.Distance(ent.PositionComp.GetPosition(), v) < distance){
							nearestPlanet = ent;
							distance = Vector3.Distance(ent.PositionComp.GetPosition(), v);
						}
					}
				}
				
				/*foreach(KeyValuePair<long, Tile> entry in planetCells){
					MyAPIGateway.Utilities.ShowMessage("SEDivers", entry.Key + "");
				}*/
				
				if(nearestPlanet != null){
					//MyAPIGateway.Utilities.ShowMessage("SEDivers", "Planet: " + nearestPlanet.EntityId);
					if(planetCells.ContainsKey(nearestPlanet.EntityId)){
						//MyAPIGateway.Utilities.ShowMessage("SEDivers", "Planet PART2: " + nearestPlanet.EntityId);
						return planetCells[nearestPlanet.EntityId];
					}
				}
			}
			
		
			//wider tile search
			int x = -100;
			int y = -100;
			try{
				v = v-center;
				x = (int)(v.X/tileSize);
				y = (int)(v.Z/tileSize);
			
				return cells[x][y];
			}
			catch(Exception e){
				//MyAPIGateway.Utilities.ShowMessage("SEDivers", "X:  " + x + " Y: " + y);
				
				return null;
			}
			
		}
		
		//gets tile, but ignores planet tiles
		private Tile getTileSpaceOnly(Vector3 v){
			int x = -100;
			int y = -100;
			try{
				v = v-center;
				x = (int)(v.X/tileSize);
				y = (int)(v.Z/tileSize);
			
				return cells[x][y];
			}
			catch(Exception e){
				//MyAPIGateway.Utilities.ShowMessage("SEDivers", "X:  " + x + " Y: " + y);
				
				return null;
			}
			
		}
		
		//get tile by x,y in grid
		public Tile getTile(int x, int y){
			try{
				return cells[x][y];
			}
			catch(Exception e){
				return null;
			}
			
		}
		
		//prepare tiles for export to save
		public List<List<List<string>>> generateOwnerSet(){
			List<List<List<string>>> lst = new List<List<List<string>>>();
			for(int i = 0; i<gridSize;i++){
				List<List<string>> miniLst = new List<List<string>>();
				for(int j = 0; j<gridSize;j++){
					List<string> nanoList = new List<string>();
					nanoList.Add(cells[i][j].owner);
					nanoList.Add(cells[i][j].getCappable());
					miniLst.Add(nanoList);
				}
				lst.Add(miniLst);
			}
			
			return lst;
		}
		
		//gets list-form tiles from save and loads them
		public void extractOwnerSet(List<List<List<string>>> lst){
			for(int i = 0; i<gridSize;i++){
				for(int j = 0; j<gridSize;j++){
					cells[i][j].owner = lst[i][j][0];
					cells[i][j].setCappable(lst[i][j][1]);
				}
			}
		}
		
		//draws a map of space tiles
		public void drawMap(){
			string result = "\nGalactic Map\n======================\n\nKEY:\n= - Freed Sector\nO - Freed Planet\nX - Occupied Planet\nA-N,Q-W,Y,Z - Occupied Sector\n\n";
			
			for(int i = gridSize-1; i>-1;i--){
				for(int j = 0; j<gridSize;j++){
					result += cells[i][j].getMapTile();
				}
				result += "\n";
			}
			
			//MyAPIGateway.Utilities.SendMessage(result);
			//MyAPIGateway.Utilities.ShowMessage("SEDivers", result);
			MyVisualScriptLogicProvider.SendChatMessage(result, "SEDivers");
		}
		
		//finds a random tile and sets it to a faction
		public void setRandomTile(string faction){
			
			int x = core.rand.Next(0, gridSize - 1);
			int y = core.rand.Next(0, gridSize - 1);
			
			getTile(x, y).setStatic(faction);
		}
		
		
		//generates dictionary for planet tiles for save file
		public List<List<string>> generatePlanetSave(){
			List<List<string>> result = new List<List<string>>();
			
			foreach(KeyValuePair<long, Tile> entry in planetCells){
				string capBoolean = "true";
				if(!entry.Value.cappable){
					capBoolean = "false";
				}
				
				List<string> tileData = new List<string>();
				tileData.Add(entry.Key.ToString());
				tileData.Add(entry.Value.owner);
				tileData.Add(capBoolean);
				
				result.Add(tileData);
			}
			
			return result;
		}
		
		//unpacks planet savedata for grid
		public void unpackPlanetSave(List<List<string>> saveData){
			
			foreach(List<string> entry in saveData){
				
				try{
					long planetID = long.Parse(entry[0]);
					
					IMyEntity ent = MyAPIGateway.Entities.GetEntityById(planetID);
				
					Tile planetTile = new Tile(ent.Name, core, core.cm.constants.capturePoints);
					
					if(planetCells.ContainsKey(planetID)){
						planetCells[planetID] = planetTile;
					}
					else{
						planetCells.Add(planetID, planetTile);
					}
				
					planetTile.owner = entry[1];
				
					if(entry[2] == "false"){
						planetTile.cappable = false;
					}
				}
				catch(Exception e){
					//MyAPIGateway.Utilities.ShowMessage("SEDivers", e.ToString());
				}
				
				
			}
			
		}
		
		//finds and updates planet locations on grid
		public void updatePlanets(){
			
			findPlanets(); //what it says on the tin
			
			//clear children from space tiles
			for(int i = 0; i<gridSize;i++){
				
				for(int j = 0; j<gridSize;j++){
					cells[i][j].clearChildren();
				}
			}
			
			//MyVisualScriptLogicProvider.SendChatMessage("UPDATING PLANETS", "TEST");
			
			//clear parents from planet tiles and build parent/child relations
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entities, null);
			
			foreach(KeyValuePair<long, Tile> entry in planetCells){
				//clear parents
				entry.Value.parent = null;
				//MyVisualScriptLogicProvider.SendChatMessage("TILE FOUND", "TEST");
				
				//find parents and then set up parent/child relations
				foreach(IMyEntity ent in entities){
					//MyVisualScriptLogicProvider.SendChatMessage("SEARCHING", "TEST");
					if(ent is MyPlanet){
						MyPlanet planet = ent as MyPlanet;
						
						//MyVisualScriptLogicProvider.SendChatMessage("PLANET FOUND", "TEST");
						
						if(planet.EntityId == entry.Key){
							Tile parentTile = getTileSpaceOnly(planet.PositionComp.GetPosition());
						
							try{
								parentTile.addChild(entry.Value);
								entry.Value.setParent(parentTile);
							}
							catch(Exception e){
								//MyVisualScriptLogicProvider.SendChatMessage(e.ToString(), "TEST");
							}
						
							break;
						}
					
					}
				}
				
				
				//old code
				/*if(/ true ){
					
					MyPlanet ent = MyAPIGateway.Entities.GetEntityById(entry.Key) as MyPlanet;
					MyVisualScriptLogicProvider.SendChatMessage("AAA", "TEST");
					
					if(ent ! null){
						MyVisualScriptLogicProvider.SendChatMessage("NULL ENTITY", "TEST");
					}
					else if(ent is MyPlanet){
						Tile parentTile = getTileSpaceOnly(ent.PositionComp.GetPosition());
						
						MyVisualScriptLogicProvider.SendChatMessage("BBB", "TEST");
						
						if(parentTile != null){
							entry.Value.setParent(parentTile);
							
							parentTile.addChild(entry.Value);
							
							//entry.Value.x = parentTile.x;
							//entry.Value.y = parentTile.y;
						}
					}
					
				}*/
				
				
				
			}
			
			//core.networker.sendMapUpdate();
			
			
			
		}
		
		
		//finds planet to add to grid
		private void findPlanets(){
			
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			
			MyAPIGateway.Entities.GetEntities(entities, null);
			
			foreach(IMyEntity ent in entities){
				if(ent is MyPlanet){
					
					if(!planetCells.ContainsKey(ent.EntityId)){
						
						Tile planetTile = new Tile(ent.Name, core, core.cm.constants.capturePoints);
				
						planetCells.Add(ent.EntityId, planetTile);
					
					}
				}
			}
			
			//old planet remover
			
			/*HashSet<long> removeQueue = new HashSet<long>();
			
			foreach(KeyValuePair<long, Tile> entry in planetCells){
				
				bool planetMissing = true;
				foreach(IMyEntity ent in entities){
					
					if(ent.EntityId == entry.Key){
						planetMissing = false;
						break;
					}
				}
				
				if(planetMissing){
					removeQueue.Add(entry.Key);
				}
				
			}
			
			foreach(long l in removeQueue){
				planetCells.Remove(l);
			}*/
		}
		
		
		//adds planets spawned
		private void OnEntityAdd(IMyEntity ent){
			if(ent != null){
				if(ent is MyPlanet && !planetCells.ContainsKey(ent.EntityId)){
				
					MyPlanet planet = ent as MyPlanet;
				
					Tile planetTile = new Tile(ent.Name, core, core.cm.constants.capturePoints);
					Tile spaceTile = getTileSpaceOnly(planet.PositionComp.GetPosition());
				
					planetTile.setParent(spaceTile);
				
					planetCells.Add(ent.EntityId, planetTile);
				
					spaceTile.addChild(planetTile);
					
					//planetTile.x = spaceTile.x;
					//planetTile.y = spaceTile.y;
				
				}
			}
		}
		
		//outputs planet list/owners
		public void generatePlanetList(){
			
			string result = "\nPlanets:\n======================\n";
			
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entities, null);
			
			foreach(KeyValuePair<long, Tile> entry in planetCells){
				
				/*MyPlanet planet = null; //MyAPIGateway.Entities.GetEntityById(entry.Key);
				
				foreach(IMyEntity ent in entities){
					
					if(ent.EntityId == entry.Key){
						
						planet = ent as MyPlanet;
						MyVisualScriptLogicProvider.SendChatMessage("PLANET FOUND", "SEDivers");
						break;
					}
				}*/
				
				string parent = "Deep Space";
				
				if(entry.Value.parent != null){
					parent = entry.Value.parent.name;
				}
				else{
					MyVisualScriptLogicProvider.SendChatMessage("NULL PARENT", "SEDivers");
				}
				
				try{
					result = result + "\n" + entry.Value.name + " (" + parent + ") : " + entry.Value.owner;
				}
				catch(Exception e){
					//MyVisualScriptLogicProvider.SendChatMessage(e.ToString(), "SEDivers");
				}
			}
			
			MyVisualScriptLogicProvider.SendChatMessage(result, "SEDivers");
			
			//MyVisualScriptLogicProvider.SendChatMessage("" + planetCells.Count, "SEDivers");
		}
		
	}
		
		
}