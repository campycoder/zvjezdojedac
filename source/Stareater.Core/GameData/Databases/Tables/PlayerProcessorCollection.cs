﻿using System;
using Stareater.Utils.Collections;
using Stareater.GameLogic;
using Stareater.Players;

namespace Stareater.GameData.Databases.Tables
{
	partial class PlayerProcessorCollection : AIndexedCollection<PlayerProcessor>
	{
		public ScalarIndex<PlayerProcessor, Player> Of { get; private set; }

		public PlayerProcessorCollection()
		{
			this.Of = new ScalarIndex<PlayerProcessor, Player>(x => x.Player);
			this.RegisterIndices(this.Of);
		}
	}
}
