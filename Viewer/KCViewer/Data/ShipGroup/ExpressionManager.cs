using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using KCViewer.Utility.Storage;

namespace KCViewer.Data.ShipGroup;

[DataContract(Name = "ExpressionManager")]
public sealed class ExpressionManager : DataStorage, ICloneable
{

	[DataMember]
	public List<ExpressionList> Expressions { get; set; }

	/// <summary>
	/// Fork: the multi-level blocks of the filter in KanColleViewer's style (KcvShipGroupFilterWindow); they replace the
	/// lists while they are set.
	/// </summary>
	[DataMember(EmitDefaultValue = false)]
	public Fork.KcvViews.KcvFilterBlock? ForkBlocks { get; set; }

	[IgnoreDataMember]
	private Expression<Func<ShipData, bool>> predicate;

	[IgnoreDataMember]
	private Expression expression;


	public ExpressionManager() : base()
	{
		Initialize();
	}

	public override void Initialize()
	{
		Expressions = new List<ExpressionList>();
		ForkBlocks = null;
		predicate = null;
		expression = null;
	}


	public ExpressionList this[int index]
	{
		get { return Expressions[index]; }
		set { Expressions[index] = value; }
	}


	public void Compile()
	{
		Expression ex = null;
		var paramex = Expression.Parameter(typeof(ShipData), "ship");

		// Fork: the blocks instead of the lists
		if (ForkBlocks is not null)
			ex = ForkBlocks.Compile(paramex);
		else
		foreach (var exlist in Expressions)
		{
			if (!exlist.Enabled)
				continue;

			if (ex == null)
			{
				ex = exlist.Compile(paramex);

			}
			else
			{
				if (exlist.ExternalAnd)
				{
					ex = Expression.AndAlso(ex, exlist.Compile(paramex));
				}
				else
				{
					ex = Expression.OrElse(ex, exlist.Compile(paramex));
				}
			}
		}


		if (ex == null)
		{
			ex = Expression.Constant(true, typeof(bool));       //:-P
		}

		predicate = Expression.Lambda<Func<ShipData, bool>>(ex, paramex);
		expression = ex;

	}


	public IEnumerable<ShipData> GetResult(IEnumerable<ShipData> list)
	{

		if (predicate == null)
			throw new InvalidOperationException("式がコンパイルされていません。");

		return list.AsQueryable().Where(predicate).AsEnumerable();
	}

	public bool IsAvailable => predicate != null;



	public override string ToString()
	{

		if (Expressions == null)
			return "(なし)";

		StringBuilder sb = new StringBuilder();
		foreach (var ex in Expressions)
		{
			if (!ex.Enabled)
				continue;
			else if (sb.Length == 0)
				sb.Append(ex.ToString());
			else
				sb.AppendFormat(" {0} {1}", ex.ExternalAnd ? "かつ" : "または", ex.ToString());
		}

		if (sb.Length == 0)
			sb.Append("(なし)");
		return sb.ToString();
	}

	public string ToExpressionString()
	{
		return expression.ToString();
	}



	public ExpressionManager Clone()
	{
		var clone = (ExpressionManager)MemberwiseClone();
		clone.Expressions = Expressions?.Select(e => e.Clone()).ToList();
		clone.ForkBlocks = ForkBlocks?.CloneBlock(); // Fork
		clone.predicate = null;
		clone.expression = null;
		return clone;
	}

	object ICloneable.Clone()
	{
		return Clone();
	}


}
