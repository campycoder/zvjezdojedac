﻿using System;
using System.Linq;
using Stareater.Galaxy;
using System.Collections.Generic;
using Stareater.Controllers.Views;
using Stareater.GameLogic;
using Stareater.GameData;
using Stareater.Players;
using Stareater.Utils;

namespace Stareater.Controllers
{
	public abstract class AConstructionSiteController
	{
		private const int NotOrder = -1;
		
		internal AConstructionSite Site { get; private set; }
		
		internal MainGame Game { get; private set; }
		internal Player Player { get; private set; }
		
		private List<int> orderIndex = new List<int>();
		
		internal AConstructionSiteController(AConstructionSite site, bool readOnly, MainGame game, Player player)
		{
			this.Site = site;
			this.IsReadOnly = readOnly;
			this.Game = game;
			this.Player = player;
		}

		public bool IsReadOnly { get; private set; }

		public SiteType SiteType
		{
			get { return Site.Type; }
		}

		internal abstract AConstructionSiteProcessor Processor { get; }
		
		public abstract IEnumerable<TraitInfo> Traits { get; }
		
		#region Buildings
		protected abstract void RecalculateSpending();

		public double DesiredSpendingRatio
		{
			get
			{
				return this.Site.Owner.Orders.ConstructionPlans[this.Site].SpendingRatio;
			}
			set
			{
				this.Site.Owner.Orders.ConstructionPlans[this.Site].SpendingRatio = Methods.Clamp(value, 0, 1);
				this.RecalculateSpending();
			}
		}

		public IEnumerable<BuildingInfo> Buildings
		{
			get
			{
				return Site.Buildings
					.Select(x => new BuildingInfo(Game.Statics.Buildings[x.Key], x.Value))
					.OrderBy(x => x.Name);
			}
		}
			
		public virtual IEnumerable<ConstructableItem> ConstructableItems
		{
			get
			{
				var playerTechs = Game.States.DevelopmentAdvances.Of[this.Player];
				var techLevels = playerTechs.ToDictionary(x => x.Topic.IdCode, x => (double)x.Level);
				var localEffencts = Processor.LocalEffects(Game.Statics).Get;

				foreach (var constructable in Game.Statics.Constructables)
					if (Prerequisite.AreSatisfied(constructable.Prerequisites, 0, techLevels) &&
						constructable.ConstructableAt == Site.Type &&
						constructable.Condition.Evaluate(localEffencts) > 0)
						yield return new ConstructableItem(constructable, Game.Derivates.Players.Of[this.Player]);
			}
		}
		
		public IEnumerable<ConstructableItem> ConstructionQueue
		{
			get
			{
				orderIndex.Clear();
				int orderI = 0;
				
				var playerProcessor = this.Game.Derivates.Of(this.Player);
				var vars = this.Processor.LocalEffects(this.Game.Statics).UnionWith(playerProcessor.TechLevels).Get;
					
				foreach(var item in Processor.SpendingPlan)
				{
					if (!item.Type.IsVirtual && item.Type == this.Player.Orders.ConstructionPlans[Site].Queue[orderI])
					{
						orderIndex.Add(orderI);
						orderI++;
					}
					else
						orderIndex.Add(NotOrder);
					
					var cost = item.Type.Cost.Evaluate(vars);
					yield return new ConstructableItem(
						item.Type, 
						Game.Derivates.Players.Of[this.Player],
						item,
						item.LeftoverPoints
					);
				}
			}
		}
		
		public bool CanPick(ConstructableItem data)
		{
			return Processor.SpendingPlan.All(x => x.Type.IdCode != data.IdCode);	//TODO(later): consider building count
		}
		
		public void Enqueue(ConstructableItem data)
		{
			if (IsReadOnly)
				return;
			
			this.Player.Orders.ConstructionPlans[Site].Queue.Add(data.Constructable);
			this.RecalculateSpending();
		}
		
		public void Dequeue(int index)
		{
			if (IsReadOnly || orderIndex[index] == NotOrder)
				return;
			
			this.Player.Orders.ConstructionPlans[Site].Queue.RemoveAt(orderIndex[index]);
			this.RecalculateSpending();
		}
		
		public void ReorderQueue(int fromIndex, int toIndex)
		{
			if (IsReadOnly || orderIndex[fromIndex] == NotOrder || orderIndex[toIndex] == NotOrder)
				return;
			
			var item = this.Player.Orders.ConstructionPlans[Site].Queue[orderIndex[fromIndex]];
			this.Player.Orders.ConstructionPlans[Site].Queue.RemoveAt(orderIndex[fromIndex]);
			this.Player.Orders.ConstructionPlans[Site].Queue.Insert(orderIndex[toIndex], item);
			this.RecalculateSpending();
		}
		#endregion
		
		public StarData HostStar
		{
			get { return Site.Location.Star; }
		}
	}
}
