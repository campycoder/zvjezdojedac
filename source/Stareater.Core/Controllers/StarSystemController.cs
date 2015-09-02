﻿using System;
using System.Collections.Generic;
using System.Linq;
using Stareater.Controllers.Views;
using Stareater.Galaxy;
using Stareater.GameData;

namespace Stareater.Controllers
{
	public class StarSystemController
	{
		public const int StarIndex = -1;
		
		private Game game;
		
		public StarData Star { get; private set; }
		public bool IsReadOnly { get; private set; }
			
		internal StarSystemController(Game game, StarData star, bool readOnly)
		{
			this.game = game;
			this.IsReadOnly = readOnly;
			this.Star = star;
		}
		
		public IEnumerable<Planet> Planets
		{
			get {
				var planetInfos = game.CurrentPlayer.Intelligence.About(Star).Planets;
				var knownPlanets = planetInfos.Where(x => x.Value.Explored == PlanetIntelligence.FullyExplored).Select(x => x.Key);
				
				return knownPlanets.OrderBy(x => x.Position);
			}
		}
		
		public ColonyInfo PlanetsColony(Planet planet)
		{
			if (game.CurrentPlayer.Intelligence.About(Star).Planets[planet].LastVisited != PlanetIntelligence.NeverVisited)
				//TODO(later): show last known colony information
				if (game.States.Colonies.AtPlanetContains(planet))
					return new ColonyInfo(game.States.Colonies.AtPlanet(planet));
			
			return null;
		}
		
		public StellarisInfo StarsAdministration()
		{
			if (game.CurrentPlayer.Intelligence.About(Star).LastVisited != StarIntelligence.NeverVisited)
				//TODO(later): show last known star system information
				return new StellarisInfo(game.States.Stellarises.At(Star));
			
			return null;
		}
		
		public BodyType BodyType(int bodyIndex)
		{
			if (bodyIndex == StarIndex) {
				if (!game.States.Stellarises.AtContains(Star))
					return Views.BodyType.NoStellarises;
					
				var stellaris = game.States.Stellarises.At(Star);
				
				return stellaris.Owner == game.CurrentPlayer ? 
					Views.BodyType.OwnStellaris : 
					Views.BodyType.ForeignStellaris;
			} 

			var planet = game.States.Planets.At(Star).FirstOrDefault(x => x.Position == bodyIndex);

			if (planet == null)
				return Views.BodyType.Empty;
			if (!game.States.Colonies.AtPlanetContains(planet))
				return Views.BodyType.NotColonised;

			var colony = game.States.Colonies.AtPlanet(planet);

			return colony.Owner == game.CurrentPlayer ? 
				Views.BodyType.OwnColony : 
				Views.BodyType.ForeignColony;
		}

		public ColonyController ColonyController(int bodyPosition)
		{
			var planet = game.States.Planets.At(Star).FirstOrDefault(x => x.Position == bodyPosition);
			
			if (planet == null)
				throw new ArgumentOutOfRangeException("bodyPosition");

			return new ColonyController(game, game.States.Colonies.AtPlanet(planet), IsReadOnly);
		}
		
		public StellarisAdminController StellarisController()
		{
			return new StellarisAdminController(game, game.States.Stellarises.At(Star), IsReadOnly);
		}
	}
}
