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
	
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation)]
	public class SosBeacon : MySessionComponentBase {
		
		private ushort port = 1442;
		
		
		public override void BeforeStart(){
            MyAPIGateway.Missiles.OnMissileCollided += OnHit;
			MyAPIGateway.Multiplayer.RegisterMessageHandler(port, OnMsg);
			
			//object o = "";
			//MyAPIGateway.Projectiles.AddOnHitInterceptor(1, new HitInterceptor(hI, hI));
        }
		
		protected override void UnloadData(){
			MyAPIGateway.Missiles.OnMissileCollided -= OnHit;
			MyAPIGateway.Multiplayer.UnregisterMessageHandler(port, OnMsg);
			//MyAPIGateway.Projectiles.AddOnHitInterceptor -= OnPHit;
		}
		
		
		private void OnMsg(byte[] byteCode){
			//try if string packet
			try{
				Payload p = MyAPIGateway.Utilities.SerializeFromBinary<Payload>(byteCode);
				
				if(p.type == PayloadType.sos){
					IMyGps sos = Helpers.generateGPS(p.contents);
					MyAPIGateway.Session.GPS.AddLocalGps(sos);
				}
			}
			catch(Exception e){
				
			}
			
		}
		
		
		private void OnHit(IMyMissile missile){
			
			if(missile.AmmoDefinition.Id.SubtypeName == "SosBeacon"){
			
				IMyGps sos = MyAPIGateway.Session.GPS.Create("SOS Beacon", "[Player Generated SOS Beacon]", missile.GetPosition(), true, false);
				//sos.Coords = missile.GetPosition();
				//sos.Name = "SOS Beacon";
				
				string sosTxt = sos.ToString();
				MyAPIGateway.Session.GPS.AddLocalGps(sos);
				
				SosPayload sp = new SosPayload();
				sp.contents = sosTxt;
				
				MyAPIGateway.Multiplayer.SendMessageToOthers(port, MyAPIGateway.Utilities.SerializeToBinary(sp), true);
				
			}
		}
		
		/*private void targetGrid(MyCubeGrid grid, MyCubeGrid gridHit){
			grid.TargetingAddId(gridHit.EntityId);
		}*/
		
	}
	
}