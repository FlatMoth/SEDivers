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

using ProtoBuf;


namespace SED {
	
	[ProtoContract]
	public class SaveFile : Payload {
		
		[ProtoMember(1)]
		public PayloadType type = PayloadType.saveFile;
		
		[ProtoMember(2)]
		public string contents;
		
		[ProtoMember(3)]
		public Vector3 center;
		
		[ProtoMember(4)]
		public List<List<List<string>>> tiles = new List<List<List<string>>>();
		
		[ProtoMember(5)]
		public string objTitle;
		
		[ProtoMember(6)]
		public List<List<string>> planets;
		
		
		public SaveFile(){
			
		}
		
		public SaveFile(Vector3 v, List<List<List<string>>> lst, string title, string desc, List<List<string>> plan){
			tiles = lst;
			center = v;
			contents = desc;
			objTitle = title;
			planets = plan;
		}
		
	}
	
}