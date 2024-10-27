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
using VRage.Game.Components.Interfaces;
using VRage.Game;
using VRage.Game.ModAPI;
using System.Collections.Concurrent;
using Sandbox.Game.EntityComponents;
using VRage.Game.Entity;


namespace SED {
	
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation)]
	public class Stratagem : MySessionComponentBase {
		
		//API base
		WcApi wcApi;
		
		//init
		public override void BeforeStart(){
			//load listeners
            MyAPIGateway.Missiles.OnMissileCollided += OnHit;
			
			//load api
			wcApi = new WcApi();
			wcApi.Load();
			
			//object o = "";
			//MyAPIGateway.Projectiles.AddOnHitInterceptor(1, new HitInterceptor(hI, hI));
        }
		
		//unloader
		protected override void UnloadData(){
			try{
				MyAPIGateway.Missiles.OnMissileCollided -= OnHit;
			
				wcApi.Unload();
			}
			catch(Exception ex){
				
			}

			//MyAPIGateway.Projectiles.AddOnHitInterceptor -= OnPHit;
		}
		
		
		
		
		//check for missile hit, if stratagem flare, target all allies at collided target
		private void OnHit(IMyMissile missile){
			
			if(missile.AmmoDefinition.Id.SubtypeName == "Stratagem"){
			
				MyEntity target = missile.CollidedEntity;
				
				Vector3 pos = missile.GetPosition();
				
				if(target is MyCubeGrid){
					MyCubeGrid gridHit = target as MyCubeGrid;
					
					HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
					
					MyAPIGateway.Entities.GetEntities(entities, null);
					
					foreach(IMyEntity e in entities){
						if(e is MyCubeGrid){
							
							MyCubeGrid grid = e as MyCubeGrid;
							
							if(grid.BigOwners.Count >= 1 && Vector3.Distance(pos, grid.PositionComp.GetPosition()) <= 15000){
								
								if(missile.IsCharacterIdFriendly(grid.BigOwners[0])){
									//MyAPIGateway.Utilities.ShowMessage("SED", missile.IsCharacterIdFriendly(grid.BigOwners[0]) + "");
									//grid.TargetingAddId(gridHit.EntityId);
									
									//vanilla behavior (lock all target locking blocks)
									foreach(IMySlimBlock block in grid.CubeBlocks){
										
										
										if(block.FatBlock is IMyDefensiveCombatBlock || block.FatBlock is IMyOffensiveCombatBlock || block.FatBlock is IMyLargeTurretBase || block.FatBlock is IMyShipController){
											/*IMyLargeTurretBase turret = block.FatBlock as IMyLargeTurretBase;
											
											turret.SetLockedTarget(gridHit);
											turret.TrackTarget(gridHit);*/
											
											MyTargetLockingBlockComponent locker = null;
											foreach(var component in block.FatBlock.Components){
												if(component is MyTargetLockingBlockComponent){
													locker = component as MyTargetLockingBlockComponent;
													break;
												}
											}
											
											if(locker != null){
												locker.OnTargetRequest(gridHit);
											}
											
											//MyAPIGateway.Utilities.ShowMessage("SED", "HIT");
										}
										
									}
									
									//wc behavior (trigger grid AI
									wcApi.SetAiFocus(grid, gridHit, 100);
								}
							}
							
						}
					}
				}
			}
		}
		
		/*private void targetGrid(MyCubeGrid grid, MyCubeGrid gridHit){
			grid.TargetingAddId(gridHit.EntityId);
		}*/
		
	}
	
}