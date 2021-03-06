﻿using System;
using Stareater.GameLogic;
using Stareater.Players;
using Stareater.Utils.Collections;

namespace Stareater.GameData
{
	partial class ResearchProgress
	{
		public const int NotStarted = -1;
		public const int Unordered = -1;
			
		public ResearchProgress(ResearchTopic topic, Player owner) : 
			this (owner, topic, NotStarted, 0)
		{ }

		public int NextLevel
		{
			get {
				if (Level < 0)
					return 0;
				else if (Level >= Topic.MaxLevel)
					return Topic.MaxLevel;
				
				return Level + 1;
			}
		}
		
		public bool CanProgress()
		{
			return Level < Topic.MaxLevel;
			
		}
		
		public void Progress(ResearchResult progressData)
		{
			this.Level += (int)progressData.CompletedCount;
			this.InvestedPoints = progressData.LeftoverPoints;
		}

		//TODO(v0.6) consider moving to player processor and merging with development progress
		//TODO(v0.6) consider merging ResearchResult and DevelopmentResearch classes
		public ResearchResult SimulateInvestment(double points)
		{
			int tmplevel = Level;
			int newLevels = 0;
			double tmpInvested = InvestedPoints;
			double totalInvested = 0;
			
			while(tmplevel < Topic.MaxLevel)
			{
				double pointsLeft = this.Topic.Cost.Evaluate(new Var(DevelopmentTopic.LevelKey, tmplevel + 1).Get) - tmpInvested;
				
				if (pointsLeft > points)
					return new ResearchResult(newLevels, totalInvested + points, this, tmpInvested + points);
				
				tmplevel++;
				newLevels++;
				
				tmpInvested = 0;
				totalInvested += pointsLeft;
				points -= pointsLeft;
			}
			
			return new ResearchResult(newLevels, totalInvested, this, tmpInvested);
		}
		
		#region Equals and GetHashCode implementation
		public override bool Equals(object obj)
		{
			var other = obj as ResearchProgress;
			if (other == null)
				return false;
			return object.Equals(this.Topic, other.Topic) && object.Equals(this.Owner, other.Owner);
		}
		
		public override int GetHashCode()
		{
			int hashCode = 0;
			unchecked {
				if (Topic != null)
					hashCode += 1000000007 * Topic.GetHashCode();
				if (Owner != null)
					hashCode += 1000000009 * Owner.GetHashCode();
			}
			return hashCode;
		}
		
		public static bool operator ==(ResearchProgress lhs, ResearchProgress rhs)
		{
			if (ReferenceEquals(lhs, rhs))
				return true;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null))
				return false;
			return lhs.Equals(rhs);
		}
		
		public static bool operator !=(ResearchProgress lhs, ResearchProgress rhs)
		{
			return !(lhs == rhs);
		}
		#endregion
	}
}
