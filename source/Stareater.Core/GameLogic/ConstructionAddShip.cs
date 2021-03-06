﻿using System;
using System.Linq;
using Stareater.Galaxy;
using Stareater.GameData.Databases;
using Stareater.Ships;
using Stareater.Ships.Missions;

namespace Stareater.GameLogic
{
	class ConstructionAddShip : IConstructionEffect
	{
		public Design Design { get; private set; }
		
		public ConstructionAddShip(Design design)
		{
			this.Design = design;
		}

		public void Apply(StatesDB states, TemporaryDB derivates, AConstructionSite site, long quantity)
		{
			//TODO(v0.6) report new ship construction
			derivates.Of(site.Owner).SpawnShip(site.Location.Star, this.Design, quantity, new AMission[0], states);
		}
		
		public void Accept(IConstructionVisitor visitor)
		{
			visitor.Visit(this);
		}
	}
}
