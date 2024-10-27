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
	public class MapPayload : Payload {
		
		[ProtoMember(1)]
		public PayloadType type = PayloadType.requestMap;
		
		[ProtoMember(2)]
		public string contents;
		
	}
	
}