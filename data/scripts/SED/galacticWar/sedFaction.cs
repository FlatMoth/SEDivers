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
	
	public class SEDFaction {
		
		public string tag = "";
		
		public string name = "";
		
		private Core core;
		
		public SEDFaction(string s, Core c){
			tag = s;
			core = c;
		}
		
		public void updateRandom(){
			
		}
		
		//moves each faction forward one tile
		public void advance(){
			
			//MyAPIGateway.Utilities.ShowMessage("SEDivers", "ADVANCING");
			
			//list of owned tiles by faction
			HashSet<Tile> ownedTiles = new HashSet<Tile>();
			//list of elligible tiles to move to
			HashSet<Tile> elligibleTiles = new HashSet<Tile>();
			
			//search space tiles to spread from
			foreach(List<Tile> entry in core.grid.cells){
				foreach(Tile t in entry){
					if(!ownedTiles.Contains(t) && t.owner == tag){
						ownedTiles.Add(t);
					}
				}
			}
			
			//search planet tiles to spread from
			foreach(KeyValuePair<long, Tile> entryPlanet in core.grid.planetCells){
				if(!ownedTiles.Contains(entryPlanet.Value) && entryPlanet.Value.owner == tag){
					ownedTiles.Add(entryPlanet.Value);
				}
			}
			
			foreach(Tile t in ownedTiles){
				List<Tile> borderTiles = new List<Tile>();
				
				if(t.x > 0){
					borderTiles.Add(core.grid.getTile(t.x-1, t.y));
				}
				if(t.x < core.grid.gridSize-1){
					borderTiles.Add(core.grid.getTile(t.x+1, t.y));
				}
				if(t.y > 0){
					borderTiles.Add(core.grid.getTile(t.x, t.y-1));
				}
				if(t.y < core.grid.gridSize-1){
					borderTiles.Add(core.grid.getTile(t.x, t.y+1));
				}
				if(t.children.Count > 0){
					foreach(Tile child in t.children){
						try{
							borderTiles.Add(child);
						}
						catch(Exception exc){
							
						}
					}
				}
				
				try{
					if(t.parent != null){
						borderTiles.Add(t.parent);
					}
				}
				catch(Exception ex){
					
				}
				
				foreach(Tile bt in borderTiles){
					if(bt == null){
						
					}
					else if(!ownedTiles.Contains(bt) && !elligibleTiles.Contains(bt) && bt.cappable){
						elligibleTiles.Add(bt);
					}
				}
			}
			
			Tile tileSelected;
			
			if(elligibleTiles.Count < 1){
				MyAPIGateway.Utilities.ShowMessage("SEDivers", "NO TILES");
				MyAPIGateway.Utilities.ShowMessage("SEDivers", "OWNED: " + ownedTiles.Count);
				return;
			}
			
			int randVal = core.rand.Next(0, 200)%(elligibleTiles.Count);
			
			tileSelected = elligibleTiles.ToArray()[randVal];
			
			tileSelected.setOwner(tag, true);
			
			//MyAPIGateway.Utilities.ShowMessage("SEDivers", "TILE SET " + tileSelected.x+", "+tileSelected.y);
		}
		
	}
		
		
}