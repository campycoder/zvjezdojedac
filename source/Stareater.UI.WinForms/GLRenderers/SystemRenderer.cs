﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using OpenTK;
using Stareater.Controllers;
using Stareater.Controllers.Views;
using Stareater.Galaxy;
using Stareater.GLData;
using Stareater.GUI;

namespace Stareater.GLRenderers
{
	class SystemRenderer : AScene
	{
		private const float DefaultViewSize = 1;
		
		private const float FarZ = 1;
		private const float Layers = 16.0f;
		
		private const float SelectionZ = 5 / Layers;
		private const float MarkColorZ = 6 / Layers;
		private const float MarkZ = 7 / Layers;
		private const float StarColorZ = 8 / Layers;
		private const float PlanetZ = 8 / Layers;
		private const float OrbitZ = 9 / Layers;
		
		private const float PanClickTolerance = 0.01f;
		
		private const float BodiesY = 0.2f;
		private const float OrbitStep = 0.3f;
		private const float OrbitOffset = 0.5f;
		private const float OrbitWidth = 0.01f;
		
		private const float OrbitPieces = 32;
		private const float StarScale = 0.5f;
		private const float PlanetScale = 0.15f;
		private const float StarSelectorScale = 1.1f;
		private const float PlanetSelectorScale = 1.1f;
		
		private const char ReturnToGalaxyKey = (char)27; //TODO(later): Make rebindable

		private StarSystemController controller;
		private PlayerController currentPlayer;
		private readonly ConstructionSiteView siteView;
		private readonly EmpyPlanetView emptyPlanetView;
		private readonly Action systemClosedHandler;

		private List<OrbitDrawable> planetOrbits = null;
		
		private Vector4? lastMousePosition = null;
		private float panAbsPath = 0;
		private float originOffset;
		private const float minOffset = -StarScale / 2;
		private float maxOffset;
		
		private int selectedBody;
		
		public SystemRenderer(Action systemClosedHandler, ConstructionSiteView siteView, EmpyPlanetView emptyPlanetView)
		{
			this.systemClosedHandler = systemClosedHandler; 
			this.emptyPlanetView = emptyPlanetView;
			this.siteView = siteView;
		}
		
		public override void Activate()
		{
			this.setupVaos();
		}
		
		public void OnNewTurn()
		{
			this.ResetLists();
		}
		
		public void SetStarSystem(StarSystemController controller, PlayerController gameController)
		{
			this.controller = controller;
			this.currentPlayer = gameController;
			
			this.maxOffset = controller.Planets.Count() * OrbitStep + OrbitOffset + PlanetScale / 2;
			
			var bestColony = controller.Planets.
				Select(x => controller.PlanetsColony(x)).
				Aggregate(
					(ColonyInfo)null, 
					(prev, next) => next == null || (prev != null && prev.Population >= next.Population) ? prev : next
				);
			this.originOffset = bestColony != null ? bestColony.Location.Position * OrbitStep + OrbitOffset : 0.5f;
			
			this.select(StarSystemController.StarIndex);
		}
		
		#region ARenderer implementation
		public override void Draw(double deltaTime)
		{
			var panTransform = Matrix4.CreateTranslation(0, BodiesY, 0);
			var starTransform = Matrix4.CreateScale(StarScale) * panTransform;

			TextureUtils.DrawSprite(GalaxyTextures.Get.SystemStar, this.projection, starTransform, StarColorZ, controller.Star.Color);
			
			if (selectedBody == StarSystemController.StarIndex)
				TextureUtils.DrawSprite(GalaxyTextures.Get.SelectedStar, this.projection, Matrix4.CreateScale(StarSelectorScale) * starTransform, SelectionZ, Color.White);
			
			//TODO(v0.6) Add texture to circle
			foreach(var orbit in this.planetOrbits)
				orbit.Draw(panTransform * this.projection);
			
			foreach(Planet planet in controller.Planets)
			{ 
				var orbitR = planet.Position * OrbitStep + OrbitOffset;
				var planetTransform = Matrix4.CreateScale(PlanetScale) * Matrix4.CreateTranslation(orbitR, 0, 0) * panTransform;
	
				switch(planet.Type)
				{
					case PlanetType.Asteriod:
						TextureUtils.DrawSprite(GalaxyTextures.Get.Asteroids, this.projection, planetTransform, PlanetZ, Color.White);
						break;
					case PlanetType.GasGiant:
						TextureUtils.DrawSprite(GalaxyTextures.Get.GasGiant, this.projection, planetTransform, PlanetZ, Color.White);
						break;
					case PlanetType.Rock:
						TextureUtils.DrawSprite(GalaxyTextures.Get.RockPlanet, this.projection, planetTransform, PlanetZ, Color.White);
						break;
				}
				
				if (this.controller.IsColonizing(planet.Position))
				{
					var markTransform = Matrix4.CreateScale(0.4f, 0.4f, 1) * Matrix4.CreateTranslation(0.6f, 0.5f, 0) * planetTransform;
					TextureUtils.DrawSprite(GalaxyTextures.Get.ColonizationMark, this.projection, markTransform, MarkZ, Color.White);
					TextureUtils.DrawSprite(GalaxyTextures.Get.ColonizationMarkColor, this.projection, markTransform, MarkColorZ, this.currentPlayer.Info.Color);
				}
				
				if (selectedBody == planet.Position)
					TextureUtils.DrawSprite(GalaxyTextures.Get.SelectedStar, this.projection, Matrix4.CreateScale(PlanetSelectorScale) * planetTransform, SelectionZ, Color.White);
			}
		}

		public override void ResetLists()
		{
			this.setupVaos();
		}

		protected override Matrix4 calculatePerspective()
		{
			var aspect = canvasSize.X / canvasSize.Y;
			return orthogonalPerspective(aspect * DefaultViewSize, DefaultViewSize, FarZ, new Vector2(originOffset, 0));
		}
		#endregion
		
		#region Input events
		public override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
		{
			switch (e.KeyChar) {
				case ReturnToGalaxyKey:
					this.systemClosedHandler();
					break;
				//TODO(later): add hotkeys for star and planets
			}
		}
		
		public override void OnMouseClick(MouseEventArgs e)
		{
			if (panAbsPath > PanClickTolerance)
				return;
			
			int? newSelection = null;
			float mouseX = Vector4.Transform(mouseToView(e.X, e.Y), invProjection).X;
			
			if (mouseX > -(OrbitOffset - OrbitStep / 2))
				newSelection = StarSystemController.StarIndex;
			
			foreach(var planet in controller.Planets)
				if (mouseX > planet.Position * OrbitStep + OrbitOffset - OrbitStep / 2)
					newSelection = planet.Position;
			
			if (newSelection.HasValue)
				select(newSelection.Value);
		}
		
		public override void OnMouseMove(MouseEventArgs e)
		{
			Vector4 currentPosition = mouseToView(e.X, e.Y);

			if (!lastMousePosition.HasValue)
				lastMousePosition = currentPosition;

			if (!e.Button.HasFlag(MouseButtons.Left)) {
				lastMousePosition = currentPosition;
				panAbsPath = 0;
				return;
			}
			
			panAbsPath += (currentPosition - lastMousePosition.Value).Length;

			originOffset -= (Vector4.Transform(currentPosition, invProjection) -
				Vector4.Transform(lastMousePosition.Value, invProjection)
				).X;

			limitPan();
			
			lastMousePosition = currentPosition;
			this.setupPerspective();
		}
		#endregion
		
		private void select(int bodyIndex)
		{
			this.selectedBody = bodyIndex;
			
			switch(controller.BodyType(bodyIndex))
			{
				case BodyType.OwnStellaris:
					siteView.SetView(controller.StellarisController());
					setView(siteView);
					break;
				case BodyType.OwnColony:
					siteView.SetView(controller.ColonyController(bodyIndex));
					setView(siteView);
					break;
				case BodyType.NotColonised:
					emptyPlanetView.SetView(controller.EmptyPlanetController(bodyIndex), currentPlayer);
					setView(emptyPlanetView);
					break;
				default:
					//TODO(later): add implementation, foregin planet, empty system, foreign system
					break;
			}
		}

		private Vector4 mouseToView(int x, int y)
		{
			return new Vector4(
				2 * x / (float)canvasSize.X - 1,
				1 - 2 * y / (float)canvasSize.Y, 
				0, 1
			);
		}
		
		private void limitPan()
		{
			if (originOffset < minOffset) 
				originOffset = minOffset;
			if (originOffset > maxOffset) 
				originOffset = maxOffset;
		}
		
		private void setView(object view)
		{
			if (emptyPlanetView.InvokeRequired)
			{
				emptyPlanetView.BeginInvoke(new Action<object>(setView), view);
				return;
			}
			
			emptyPlanetView.Visible = view.Equals(emptyPlanetView);
			siteView.Visible = view.Equals(siteView);
		}
		
		private void setupVaos()
		{
			var batchData = new List<PlanetOrbitGlProgram.ObjectData>();
			var vaoBuilder = new VertexArrayBuilder();
			
			foreach(Planet planet in controller.Planets)
			{ 
				var orbitR = planet.Position * OrbitStep + OrbitOffset;
				var orbitMin = orbitR - OrbitWidth * 3;
				var orbitMax = orbitR + OrbitWidth * 3;
				
				vaoBuilder.BeginObject();
				for(int i = 0; i < OrbitPieces; i++)
				{
					var angle0 = 2 * (float)Math.PI * i / OrbitPieces;
					var angle1 = 2 * (float)Math.PI * (i +1) / OrbitPieces;
					
					vaoBuilder.AddOrbitVertex(orbitMin * (float)Math.Cos(angle1), orbitMin * (float)Math.Sin(angle1));
					vaoBuilder.AddOrbitVertex(orbitMax * (float)Math.Cos(angle1), orbitMax * (float)Math.Sin(angle1));
					vaoBuilder.AddOrbitVertex(orbitMax * (float)Math.Cos(angle0), orbitMax * (float)Math.Sin(angle0));
					
					vaoBuilder.AddOrbitVertex(orbitMax * (float)Math.Cos(angle0), orbitMax * (float)Math.Sin(angle0));
					vaoBuilder.AddOrbitVertex(orbitMin * (float)Math.Cos(angle0), orbitMin * (float)Math.Sin(angle0));
					vaoBuilder.AddOrbitVertex(orbitMin * (float)Math.Cos(angle1), orbitMin * (float)Math.Sin(angle1));
				}
				vaoBuilder.EndObject();
				
				var colony = controller.PlanetsColony(planet);
				batchData.Add(new PlanetOrbitGlProgram.ObjectData(
					OrbitZ,
					orbitR - OrbitWidth / 2,
					orbitR + OrbitWidth / 2,
					colony != null ? colony.Owner.Color : Color.FromArgb(64, 64, 64),
					Matrix4.Identity 
				));
			}
			
			VertexArray starVao;
			if (this.planetOrbits == null)
			{
				this.planetOrbits = new List<OrbitDrawable>();
				starVao = vaoBuilder.Generate(ShaderLibrary.PlanetOrbit);
			}
			else
			{
				//TODO(v0.6) Not guarenteed to have any elements, make batch drawable
				starVao = this.planetOrbits[0].Vao;
				vaoBuilder.Update(starVao);
				this.planetOrbits.Clear();
			}
			
			for(int i = 0; i < batchData.Count; i++)
				this.planetOrbits.Add(new OrbitDrawable(starVao, i, batchData[i]));
		}
	}
}
