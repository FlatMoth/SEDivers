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
	
	public class Tile {
		
		//capture point variables
		public int capProgress;
		public int pointsToCapture;
		
		//owner faction name
		public string owner = "PLAYER";
		
		//lock for capturing
		public bool cappable = true;
		
		//basic info
		public string name;
		public int x = -1;
		public int y = -1;
		
		//parent and child tiles. useful for planet sector functionality
		public Tile parent;
		public List<Tile> children = new List<Tile>();
		
		//main
		private Core core;
		
		public Tile(){
			
		}
		
		//init
		public Tile(int xt, int yt, Core c, int points){
			capProgress = points;
			pointsToCapture = points;
			core = c;
			x=xt;
			y=yt;
			name = (xt+1) + "-" + (yt+1);
		}
		
		
		//init, but for use on planet tiles
		public Tile(string planetName, Core c, int points){
			capProgress = points;
			pointsToCapture = points;
			core = c;
			//x=xt;
			//y=yt;
			name = planetName.Split('-')[0];
		}
		
		//reset owner to player
		public void liberate(){
			
			if(!core.allowDynamicMap){
				return;
			}
			
			if(parent != null){
				parent.liberate();
			}
			
			setOwner("PLAYER", true);
		}
		
		//toggle capping lock
		public void setCappable(string s){
			if(s == "true"){
				cappable = true;
			}
			else{
				cappable = false;
			}
		}
		
		//get capping lock state
		public string getCappable(){
			if(cappable){
				return "true";
			}
			else{
				return "false";
			}
		}
		
		//get map tile icon
		public string getMapTile(){
			
			if(owner == "PLAYER"){
				if(children.Count > 0){
					bool isChildHeld = false;
					
					try{
						foreach(Tile child in children){
							if(child.owner != "PLAYER"){
								isChildHeld = true;
							}
						}
					}
					catch(Exception exc){
						
					}
				
					if(!isChildHeld){
						return "O";
					}
					else{
						return "X";
					}
					
				}
				else{
					return "=";
				}
			}
			else{
				
				bool isChildHeld = false;
					
				try{
					foreach(Tile child in children){
						if(child.owner != "PLAYER"){
							isChildHeld = true;
						}
					}
				}
				catch(Exception exc){
						
				}
				
				if(isChildHeld){
					return "X";
				}
				
				try{
					string firstLetter = owner[0] + "";
					
					if(firstLetter == "X" || firstLetter == "O" || firstLetter == "="){
						return "E";
					}
					
					return firstLetter;
				}
				catch(Exception ex){
					return "E";
				}
			}
		}
		
		//change tile owner
		public void setOwner(string s, bool announce){
			capProgress = pointsToCapture;
			owner = s;
			
			if(!core.allowDynamicMap){
				return;
			}
			
			if(announce){
				if(s != "PLAYER"){
					//child.setOwner(s, true);
					//foreach(Tile t in children){
					//	t.setOwner(s, true);
					//}
				}
				
				core.networker.sendTileCap(s, name);
			}
			core.networker.sendMapUpdate();
			
		}
		
		//set ownership and lock cap state
		public void setStatic(string s){
			cappable = false;
			setOwner(s, false);
		}
		
		//tick cap progress
		public void tickCap(){
			capProgress--;
		}
		
		//update cycle
		public void update(){
			if(capProgress <= 0 && cappable){
				if(owner == "PLAYER"){
					
				}
				else{
					liberate();
				}
			}
			else {
				capProgress = pointsToCapture;
			}
		}
		
		//sets child tile. useful for space around a planet
		public void addChild(Tile t){
			children.Add(t);
		}
		
		//erases children list
		public void clearChildren(){
			children = new List<Tile>();
		}
		
		//sets parent tile. useful for planet tiles
		public void setParent(Tile t){
			parent = t;
		}
		
		
	}
		
		
}