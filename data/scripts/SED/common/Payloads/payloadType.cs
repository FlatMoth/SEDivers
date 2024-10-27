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
	
	public enum PayloadType {
		sos, //sos beacon
		missionComplete, //mission completion
		tileCap, //tile capture
		requestMap, //request tile map
		setCenter, //set world center
		gpsPayload, //update priority sectors on client
		gpsPayloadRequest, //ask server to send priority sector update
		saveFile, //identifies savegame
		setTile, //request tile ownership change (by command)
		modConfig, //identifies a mod config
		getCurrentTile, //requests current tile
		autoPopulate, //request to populate tiles
		getCenter, //sends center to calculate tile from
		fastSetup, //request to perform fastsetup
		newObjective, //sends objective update between players
		objectiveComplete, //sends out alert to pay all players for obj. completion
		getPlanets, //retrieves planet owners
		requestMapSize,
		sendMapSize, //request and send row and column size of map.
		toggleWC //toggle WC compatibility
	}
	
}