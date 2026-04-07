using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XRL.Rules;

namespace Genkit;

public abstract class WaveCollapseFastModelBase
{
	public static readonly int INIT_SIZE = 350;

	protected static BitArray[] wave;

	protected static List<int>[][] propagator;

	private static int[][][] compatible;

	private static Tuple<int, int>[] stack;

	private int stacksize;

	protected System.Random random = Stat.Rnd5;

	public int FMX;

	public int FMY;

	public int T;

	protected bool periodic;

	protected double[] weights;

	private static double[] weightLogWeights;

	protected double sumOfWeights;

	protected double sumOfWeightLogWeights;

	protected double startingEntropy;

	protected static int[] observed;

	private static int[] sumsOfOnes;

	protected static double[] sumsOfWeights;

	protected static double[] sumsOfWeightLogWeights;

	protected static double[] entropies;

	private static List<double> distribution = new List<double>(256);

	private static Stack<Tuple<int, int>> tuplePool = new Stack<Tuple<int, int>>();

	protected static int[] DX = new int[4] { -1, 0, 1, 0 };

	protected static int[] DY = new int[4] { 0, 1, 0, -1 };

	private static int[] opposite = new int[4] { 2, 3, 0, 1 };

	protected WaveCollapseFastModelBase(int width, int height)
	{
		FMX = width;
		FMY = height;
	}

	public abstract Color32[] GetResult();

	protected void Init()
	{
		Debug.Log("Init T=" + T);
		int num = Math.Max(T, INIT_SIZE);
		if (wave == null)
		{
			wave = new BitArray[18000];
		}
		if (compatible == null)
		{
			compatible = new int[wave.Length][][];
		}
		for (int i = 0; i < wave.Length; i++)
		{
			if (wave[i] == null || wave[i].Length < T)
			{
				wave[i] = new BitArray(num);
			}
			if (compatible[i] == null || compatible[i].Length < T)
			{
				compatible[i] = new int[num][];
				for (int j = 0; j < num; j++)
				{
					compatible[i][j] = new int[4];
				}
			}
		}
		if (weightLogWeights == null || weightLogWeights.Length < T)
		{
			Debug.Log("Reinitializing weightLogWeights to T=" + num);
			weightLogWeights = new double[num];
		}
		sumOfWeights = 0.0;
		sumOfWeightLogWeights = 0.0;
		for (int k = 0; k < T; k++)
		{
			weightLogWeights[k] = weights[k] * Math.Log(weights[k]);
			sumOfWeights += weights[k];
			sumOfWeightLogWeights += weightLogWeights[k];
		}
		startingEntropy = Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;
		if (sumsOfOnes == null)
		{
			sumsOfOnes = new int[wave.Length];
		}
		if (sumsOfWeights == null)
		{
			sumsOfWeights = new double[wave.Length];
		}
		if (sumsOfWeightLogWeights == null)
		{
			sumsOfWeightLogWeights = new double[wave.Length];
		}
		if (entropies == null)
		{
			entropies = new double[wave.Length];
		}
		if (observed == null)
		{
			observed = new int[wave.Length];
		}
		if (stack == null || stack.Length < wave.Length * T)
		{
			Debug.Log("Reinitializing stack to T=" + num);
			stack = new Tuple<int, int>[wave.Length * num];
		}
		stacksize = 0;
	}

	private bool? Observe()
	{
		double num = 1000.0;
		int num2 = -1;
		for (int i = 0; i < wave.Length; i++)
		{
			if (OnBoundary(i % FMX, i / FMX))
			{
				continue;
			}
			int num3 = sumsOfOnes[i];
			if (num3 == 0)
			{
				return false;
			}
			double num4 = entropies[i];
			if (num3 > 1 && num4 <= num)
			{
				double num5 = 1E-06 * random.NextDouble();
				if (num4 + num5 < num)
				{
					num = num4 + num5;
					num2 = i;
				}
			}
		}
		if (num2 == -1)
		{
			for (int j = 0; j < wave.Length; j++)
			{
				for (int k = 0; k < T; k++)
				{
					if (wave[j][k])
					{
						observed[j] = k;
						break;
					}
				}
			}
			return true;
		}
		distribution.Clear();
		for (int l = 0; l < T; l++)
		{
			distribution.Add(wave[num2][l] ? weights[l] : 0.0);
		}
		int num6 = distribution.Random(random.NextDouble());
		BitArray bitArray = wave[num2];
		for (int m = 0; m < T; m++)
		{
			if (bitArray[m] != (m == num6))
			{
				Ban(num2, m);
			}
		}
		return null;
	}

	protected void Propagate()
	{
		while (stacksize > 0)
		{
			Tuple<int, int> tuple = stack[stacksize - 1];
			stack[stacksize - 1] = null;
			stacksize--;
			int item = tuple.Item1;
			int num = item % FMX;
			int num2 = item / FMX;
			for (int i = 0; i < 4; i++)
			{
				int num3 = DX[i];
				int num4 = DY[i];
				int num5 = num + num3;
				int num6 = num2 + num4;
				if (OnBoundary(num5, num6))
				{
					continue;
				}
				if (num5 < 0)
				{
					num5 += FMX;
				}
				else if (num5 >= FMX)
				{
					num5 -= FMX;
				}
				if (num6 < 0)
				{
					num6 += FMY;
				}
				else if (num6 >= FMY)
				{
					num6 -= FMY;
				}
				int num7 = num5 + num6 * FMX;
				List<int> list = propagator[i][tuple.Item2];
				int[][] array = compatible[num7];
				for (int j = 0; j < list.Count; j++)
				{
					int num8 = list[j];
					int[] obj = array[num8];
					obj[i]--;
					if (obj[i] == 0)
					{
						Ban(num7, num8);
					}
				}
			}
			tuplePool.Push(tuple);
		}
	}

	public virtual bool Run(int seed, int limit)
	{
		Init();
		Clear();
		random = new System.Random(seed);
		for (int i = 0; i < limit || limit == 0; i++)
		{
			bool? flag = Observe();
			if (flag.HasValue)
			{
				return flag.Value;
			}
			Propagate();
		}
		return true;
	}

	protected void Ban(int i, int t)
	{
		wave[i][t] = false;
		int[] array = compatible[i][t];
		for (int j = 0; j < 4; j++)
		{
			array[j] = 0;
		}
		if (tuplePool.Count > 0)
		{
			stack[stacksize] = tuplePool.Pop();
			stack[stacksize] = new Tuple<int, int>(i, t);
		}
		else
		{
			stack[stacksize] = new Tuple<int, int>(i, t);
		}
		stacksize++;
		double num = sumsOfWeights[i];
		entropies[i] += sumsOfWeightLogWeights[i] / num - Math.Log(num);
		sumsOfOnes[i]--;
		sumsOfWeights[i] -= weights[t];
		sumsOfWeightLogWeights[i] -= weightLogWeights[t];
		num = sumsOfWeights[i];
		entropies[i] -= sumsOfWeightLogWeights[i] / num - Math.Log(num);
	}

	protected virtual void Clear()
	{
		for (int i = 0; i < wave.Length; i++)
		{
			for (int j = 0; j < T; j++)
			{
				wave[i][j] = true;
				for (int k = 0; k < 4; k++)
				{
					compatible[i][j][k] = propagator[opposite[k]][j].Count;
				}
			}
			sumsOfOnes[i] = weights.Length;
			sumsOfWeights[i] = sumOfWeights;
			sumsOfWeightLogWeights[i] = sumOfWeightLogWeights;
			entropies[i] = startingEntropy;
			observed[i] = 0;
		}
	}

	protected abstract bool OnBoundary(int x, int y);
}
