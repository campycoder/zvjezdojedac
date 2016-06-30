﻿using System;
using System.Collections.Generic;
using System.Linq;
using NGenerics.DataStructures.Mathematical;
using Stareater.Controllers.Data;
using Stareater.Controllers.Views;
using Stareater.Controllers.Views.Ships;
using Stareater.Galaxy;
using Stareater.GameData;
using Stareater.Players;
using Stareater.Utils;

namespace Stareater.Controllers
{
	public class PlayerController
	{
		public int PlayerIndex { get; private set; }
		private GameController gameController;
		
		private GalaxyObjects mapCache = new GalaxyObjects();
		private IVisualPositioner visualPositioner = null;
		
		internal PlayerController(int playerIndex, GameController gameController)
		{
			this.PlayerIndex = playerIndex;
			this.gameController = gameController;
		}
		
		internal Player PlayerInstance
		{
			get { return this.gameController.GameInstance.Players[this.PlayerIndex]; }
		}
		
		private MainGame gameInstance
		{
			get { return this.gameController.GameInstance; }
		}
		
		public PlayerInfo Info
		{
			get { return new PlayerInfo(this.PlayerInstance); }
		}

		public LibraryController Library 
		{
			get { return new LibraryController(this.gameController); }
		}
		
		#region Turn progression
		public void EndGalaxyPhase()
		{
			this.gameController.EndGalaxyPhase(this);
		}

		public void EndCombatPhase()
		{
			this.gameController.EndCombatPhase();
		}

		public bool IsReadOnly
		{
			get { return this.gameController.IsReadOnly; }
		}
		#endregion
			
		#region Map related
		public IVisualPositioner VisualPositioner
		{ 
			get { return this.visualPositioner; }
			set
			{
				this.visualPositioner = value;
				this.RebuildCache();
			}
		}
		
		public bool IsStarVisited(StarData star)
		{
			return this.PlayerInstance.Intelligence.About(star).IsVisited;
		}
		
		public IEnumerable<ColonyInfo> KnownColonies(StarData star)
		{
			var game = this.gameInstance;
			var starKnowledge = this.PlayerInstance.Intelligence.About(star);
			
			foreach(var colony in game.States.Colonies.AtStar(star))
				if (starKnowledge.Planets[colony.Location.Planet].LastVisited != PlanetIntelligence.NeverVisited)
					yield return new ColonyInfo(colony);
		}
		
		public StarSystemController OpenStarSystem(StarData star)
		{
			//TODO(v0.5) pass player
			return new StarSystemController(this.gameInstance, star, this.IsReadOnly, this.PlayerInstance);
		}
		
		public StarSystemController OpenStarSystem(Vector2D position)
		{
			//TODO(v0.5) pass player
			return this.OpenStarSystem(this.gameInstance.States.Stars.At(position));
		}
		
		public GalaxySearchResult FindClosest(float x, float y, float searchRadius)
		{

			return this.mapCache.Search(x, y, searchRadius);
		}
		
		public FleetController SelectFleet(FleetInfo fleet)
		{
			return new FleetController(fleet, this.gameInstance, this.PlayerInstance, this.mapCache, this.VisualPositioner);
		}
		
		public IEnumerable<FleetInfo> Fleets
		{
			get
			{
				return this.mapCache.Fleets;
			}
		}
		
		public StarData Star(Vector2D position)
		{
			return this.gameInstance.States.Stars.At(position);
		}
		
		public int StarCount
		{
			get 
			{
				return this.gameInstance.States.Stars.Count;
			}
		}
		
		public IEnumerable<StarData> Stars
		{
			get
			{
				return this.gameInstance.States.Stars;
			}
		}

		public IEnumerable<Wormhole> Wormholes
		{
			get
			{
				foreach (var wormhole in this.gameInstance.States.Wormholes)
					yield return wormhole;
			}
		}

		internal void RebuildCache()
		{
			var fleets = new List<FleetInfo>();

			//TODO(later) filter invisible fleets
			foreach (var fleet in this.gameInstance.States.Fleets) {
				if (fleet.Owner == this.PlayerInstance && this.PlayerInstance.Orders.ShipOrders.ContainsKey(fleet.Position))
					foreach(var newFleet in this.PlayerInstance.Orders.ShipOrders[fleet.Position])
						fleets.Add(new FleetInfo(
							newFleet, 
							this.gameInstance.States.Stars.AtContains(fleet.Position), 
							this.VisualPositioner, 
							this.gameInstance.Derivates.Of(fleet.Owner),
							this.gameInstance.Statics
						));
				else
					fleets.Add(new FleetInfo(
						fleet, 
						this.gameInstance.States.Stars.AtContains(fleet.Position), 
						this.VisualPositioner, 
						this.gameInstance.Derivates.Of(fleet.Owner),
						this.gameInstance.Statics
					));
			}

			this.mapCache.Rebuild(this.gameInstance.States.Stars, fleets);
		}
		#endregion
		
		#region Stellarises and colonies
		public IEnumerable<StellarisInfo> Stellarises()
		{
			foreach(var stellaris in this.gameInstance.States.Stellarises.OwnedBy(this.PlayerInstance))
				yield return new StellarisInfo(stellaris);
		}
		#endregion
		
		#region Ship designs
		public ShipDesignController NewDesign()
		{
			return new ShipDesignController(this.gameInstance, this.PlayerInstance); //FIXME(v0.5) check if the game is read only 
		}
		
		public IEnumerable<DesignInfo> ShipsDesigns()
		{
			var game = this.gameInstance;
			return game.States.Designs.
				OwnedBy(this.PlayerInstance).
				Select(x => new DesignInfo(x, game.Derivates.Of(this.PlayerInstance).DesignStats[x], game.Statics));
		}
		#endregion
		
		#region Colonization related
		public IEnumerable<ColonizationController> ColonizationProjects()
		{
			var planets = new HashSet<Planet>();
			planets.UnionWith(this.gameInstance.States.ColonizationProjects.OwnedBy(this.PlayerInstance).Select(x => x.Destination));
			planets.UnionWith(this.PlayerInstance.Orders.ColonizationOrders.Keys);
			
			foreach(var planet in planets)
				yield return new ColonizationController(this.gameInstance, planet, this.IsReadOnly, this.PlayerInstance);
		}
		
		public IEnumerable<FleetInfo> EnrouteColonizers(Planet destination)
		{
			var finder = new ColonizerFinder(destination);
			
			foreach(var fleet in mapCache.Fleets.Where(x => x.Owner.Data == this.PlayerInstance))
				if (finder.Check(fleet.FleetData))
					yield return fleet;
		}
		#endregion
		
		#region Development related
		public IEnumerable<TechnologyTopic> DevelopmentTopics()
		{
			var game = this.gameInstance;
			var playerTechs = game.Derivates.Of(this.PlayerInstance).DevelopmentOrder(game.States.TechnologyAdvances);
		
			if (game.Derivates.Of(this.PlayerInstance).DevelopmentPlan == null)
				game.Derivates.Of(this.PlayerInstance).CalculateDevelopment(
					game.Statics,
					game.States,
					game.Derivates.Colonies.OwnedBy(this.PlayerInstance)
				);
			
			var investments = game.Derivates.Of(this.PlayerInstance).DevelopmentPlan.ToDictionary(x => x.Item);
			
			foreach(var techProgress in playerTechs)
				if (investments.ContainsKey(techProgress))
					yield return new TechnologyTopic(techProgress, investments[techProgress]);
				else
					yield return new TechnologyTopic(techProgress);
			
		}
		
		public IEnumerable<TechnologyTopic> ReorderDevelopmentTopics(IEnumerable<string> idCodeOrder)
		{
			if (this.IsReadOnly)
				return DevelopmentTopics();

			var modelQueue = this.PlayerInstance.Orders.DevelopmentQueue;
			modelQueue.Clear();
			
			int i = 0;
			foreach (var idCode in idCodeOrder) {
				modelQueue.Add(idCode, i);
				i++;
			}
			
			this.gameInstance.Derivates.Of(this.PlayerInstance).InvalidateDevelopment();
			return DevelopmentTopics();
		}
		
		public DevelopmentFocusInfo[] DevelopmentFocusOptions()
		{
			return this.gameInstance.Statics.DevelopmentFocusOptions.Select(x => new DevelopmentFocusInfo(x)).ToArray();
		}
		
		public int DevelopmentFocusIndex 
		{ 
			get
			{
				return this.PlayerInstance.Orders.DevelopmentFocusIndex;
			}
			
			set
			{
				if (this.IsReadOnly)
					return;
				
				if (value >= 0 && value < this.gameInstance.Statics.DevelopmentFocusOptions.Count)
					this.PlayerInstance.Orders.DevelopmentFocusIndex = value;
				
				this.gameInstance.Derivates.Of(this.PlayerInstance).InvalidateDevelopment();
			}
		}
		
		public double DevelopmentPoints 
		{ 
			get
			{
				var game = this.gameInstance;
				
				return game.Derivates.Colonies.OwnedBy(this.PlayerInstance).Sum(x => x.Development);
			}
		}
		#endregion
		
		#region Research related
		public IEnumerable<TechnologyTopic> ResearchTopics()
		{
			var game = this.gameInstance;
			var playerTechs = game.Derivates.Of(this.PlayerInstance).ResearchOrder(game.States.TechnologyAdvances);
		
			if (game.Derivates.Of(this.PlayerInstance).ResearchPlan == null)
				game.Derivates.Of(this.PlayerInstance).CalculateResearch(
					game.Statics,
					game.States,
					game.Derivates.Colonies.OwnedBy(this.PlayerInstance)
				);
			
			var investments = game.Derivates.Of(this.PlayerInstance).ResearchPlan.ToDictionary(x => x.Item);
			
			foreach(var techProgress in playerTechs)
				if (investments.ContainsKey(techProgress))
					yield return new TechnologyTopic(techProgress, investments[techProgress]);
				else
					yield return new TechnologyTopic(techProgress);
		}
		
		public int ResearchFocus
		{
			get 
			{
				var game = this.gameInstance;
				string focused = this.PlayerInstance.Orders.ResearchFocus;
				var playerTechs = game.Derivates.Of(this.PlayerInstance).ResearchOrder(game.States.TechnologyAdvances).ToList();
				
				for (int i = 0; i < playerTechs.Count; i++)
					if (playerTechs[i].Topic.IdCode == focused)
						return i;
				
				return 0; //TODO(later) think of some smarter default research
			}
			
			set
			{
				if (this.IsReadOnly)
					return;
				
				var playerTechs = this.gameInstance.Derivates.Of(this.PlayerInstance).ResearchOrder(this.gameInstance.States.TechnologyAdvances).ToList();
				if (value >= 0 && value < playerTechs.Count) {
					this.PlayerInstance.Orders.ResearchFocus = playerTechs[value].Topic.IdCode;
					this.gameInstance.Derivates.Of(this.PlayerInstance).InvalidateResearch();
				}
			}
		}
		
		public StarData ResearchCenter
		{
			get { return this.gameInstance.Derivates.Of(this.PlayerInstance).ResearchCenter; }
		}
		#endregion
		
		#region Report related
		public IEnumerable<IReportInfo> Reports
		{
			get {
				var game = this.gameInstance;
				var wrapper = new ReportWrapper();
				
				foreach(var report in game.States.Reports.Of(this.PlayerInstance))
					yield return wrapper.Wrap(report);
			}
		}
		#endregion
	}
}
