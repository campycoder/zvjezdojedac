﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Stareater.AppData.Expressions
{
	public class Formula
	{
		private IExpressionNode root;
		private ISet<string> variables = null;

		internal Formula(IExpressionNode root)
		{
			this.root = root;
		}

		internal Formula(double constValue) : this(new Constant(constValue))
		{ }
		
		internal Formula(bool constCondition) : this(new Constant(constCondition ? 1 : -1))
		{ }
		
		public double Evaluate(IDictionary<string, double> variables)
		{
#if DEBUG
			try {
#endif
			return root.Evaluate(variables);
#if DEBUG
			}
			catch(KeyNotFoundException)
			{
				System.Diagnostics.Trace.WriteLine("Missing vars: " + string.Join(" ", this.Variables.Where(x => !variables.ContainsKey(x)).ToArray()));
				throw;
			}
#endif
		}
		
		public ISet<string> Variables 
		{
			get 
			{
				if (variables == null)
					variables = new HashSet<string>(root.Variables);
				
				return variables;
			}
		}
	}
}
