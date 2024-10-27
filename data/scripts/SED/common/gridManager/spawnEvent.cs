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
using VRage.ModAPI;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Planet;
using System.Numerics;
using SpaceEngineers.Game.ModAPI;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI.Ingame.Utilities;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.Entities.Blocks;


namespace SED {
	
	public class SpawnEvent {
		
		private Core core;
		
		private List<IMyCubeGrid> closeQueue = new List<IMyCubeGrid>();
		private List<IMyFunctionalBlock> disabledAntennas = new List<IMyFunctionalBlock>();
		
		public SpawnEvent(Core main){
			core = main;
			MyVisualScriptLogicProvider.PrefabSpawnedDetailed += onSpawn;
		}
		
		public void stop(){
			MyVisualScriptLogicProvider.PrefabSpawnedDetailed -= onSpawn;
		}
		
		
		private void onSpawn(long id, string name){
			
			IMyEntity ent = MyAPIGateway.Entities.GetEntityById(id);
			if(ent == null || !(ent is MyCubeGrid)){
				return;
			}
			MyCubeGrid grid = ent as MyCubeGrid;
			
			//MyAPIGateway.Utilities.ShowMessage("SEDivers", "Spawning Grid:  " + ent.DisplayName);
			
			if(grid.BigOwners.Count < 1){
				return;
			}
			IMyFaction gridFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(grid.BigOwners[0]);
			Tile gridTile = core.grid.getTile(ent.GetPosition());
			if(gridFaction == null){
				return;
			}
			//MyAPIGateway.Utilities.ShowMessage("SEDivers", "Faction:  " + gridFaction.Tag);
			//MyAPIGateway.Utilities.ShowMessage("SEDivers", "contained:  " + core.factions.ContainsKey(gridFaction.Tag));
			
			if((core.factions.ContainsKey(gridFaction.Tag) || core.friendlies.Contains(gridFaction.Tag)) && gridTile == null){
				removeEntity(ent);
				return;
			}
			
			if(core.friendlies.Contains(gridFaction.Tag) && gridTile.owner != "PLAYER"){
				removeEntity(ent);
				return;
			}
			
			
			if(core.factions.ContainsKey(gridFaction.Tag) && gridTile.owner != gridFaction.Tag){
				removeEntity(ent);
				return;
			}
			
			foreach(IMySlimBlock block in grid.CubeBlocks){
				IMyFunctionalBlock fatBlock = block.FatBlock as IMyFunctionalBlock;
				if((fatBlock is IMyBeacon || fatBlock is IMyRadioAntenna) && fatBlock.Enabled){
					fatBlock.Enabled = false;
					disabledAntennas.Add(fatBlock);
				}
			}
			
			/*bool isPowered = false;
			foreach(IMySlimBlock block in grid.CubeBlocks){
				IMyCubeBlock fatBlock = block.FatBlock;
				if(fatBlock is IMyPowerProducer || fatBlock is IMyUserControllableGun || fatBlock is IMyLargeTurretBase || fatBlock is IMyConveyorSorter || fatBlock is IMyCockpit || fatBlock is IMyConveyor || fatBlock is IMyConveyorTube){
					isPowered = true;
					
					break;
				}
			}
			if(!isPowered){
				removeEntity(ent);
				return;
			}*/
			
			//IMyCubeGrid igrid = grid as IMyCubeGrid;
			
			
			
			
			/*if(core.aggroShips.ContainsKey(name)){
				core.targets.Add(grid, gridTile);
			}*/
			
			
			/*if(core.aggroLst.ContainsKey(name)){
				
				
				//MyAPIGateway.Utilities.ShowMessage("Station Framework",  "aggro ship spawned!!!");
				//get grid by id
				IMyEntity ent = MyAPIGateway.Entities.GetEntityById(id);
				//get entity loc
				Vector3 spawnLoc = ent.PositionComp.GetPosition();
				
				int dist = core.aggroLst[name];
				
				if(ent != null){
					
					if(core.checkProximity(spawnLoc, dist) && ent is IMyCubeGrid){
						
						
						core.replaceGrid(ent as IMyCubeGrid, spawnLoc);
						
					}
				
				}
				
			}*/
		}
		
		public void removeEntity(IMyEntity ent){
			
			try{
				if(ent is IMyCubeGrid){
				
					IMyCubeGrid grid = ent as IMyCubeGrid;
				
					/*var gridGroup = grid.GetGridGroup(GridLinkTypeEnum.Physical);
					List<IMyCubeGrid> grids = new List<IMyCubeGrid>();
					gridGroup.GetGrids(grids);
				
					foreach(IMyCubeGrid subgrid in grids){
						if(!closeQueue.Contains(subgrid)){
							closeQueue.Add(subgrid);
						}
					}*/
					
					if(!closeQueue.Contains(grid)){
						closeQueue.Add(grid);
					}
				
				}
				else{
					MyAPIGateway.Entities.MarkForClose(ent);
				}
			}
			catch(Exception ex){
				
			}
			
			//old code
			/*try{
				if(ent is IMyCubeGrid){
					
					IMyCubeGrid grid = ent as IMyCubeGrid;
				
					//var gridGroup = grid.GetGridGroup(GridLinkTypeEnum.Physical);
					//List<IMyCubeGrid> grids = new List<IMyCubeGrid>();
					//gridGroup.GetGrids(grids);
					
					
					
					foreach(IMyCubeGrid g in grids){
						//MyAPIGateway.Entities.MarkForClose(g);
						//closeQueue.Add(g);
					}
				}
				else{
					MyAPIGateway.Entities.MarkForClose(ent);
				}
			}
			catch(Exception e){
				//MyAPIGateway.Utilities.ShowMessage("SEDivers", e.ToString() + "");
			}*/
			
			
			//MyAPIGateway.Utilities.ShowMessage("SEDivers", "Removing Grid");
		}
		
		public void clearQueue(){
			
			//List<IMyCubeGrid> removeTemp = new List<IMyCubeGrid>();
			
			foreach(IMyCubeGrid grid in closeQueue){
				try{
					//MyAPIGateway.Entities.MarkForClose(grid);
					
					var gridGroup = grid.GetGridGroup(GridLinkTypeEnum.Physical);
					List<IMyCubeGrid> grids = new List<IMyCubeGrid>();
					gridGroup.GetGrids(grids);
				
					foreach(IMyCubeGrid subgrid in grids){
						MyAPIGateway.Entities.MarkForClose(subgrid);
					}
				}
				catch(Exception ex){
					
				}
			}
			
		}
		
		public void activateAntennas(){
			foreach(IMyFunctionalBlock block in disabledAntennas){
				
				//IMyFunctionalBlock fb = block as IMyFunctionalBlock;
				
				block.Enabled = true;
				
			}
			
			disabledAntennas = new List<IMyFunctionalBlock>();
		}
		
	}
	
}