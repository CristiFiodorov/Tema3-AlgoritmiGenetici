﻿using Tema3;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Data.SQLite;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Tema3
{
	class Program
	{
		static double meanValue = 0;
		static List<double> values = new List<double>();
		static object sumlock = new object();

		static double meanTimestamp = 0;
		static List<long> Timestamps = new List<long>();
		
		static SQLiteConnection connection = new SQLiteConnection("Data Source=C:\\Genetici\\Tema3.db");


		
		static void Main(string[] args)
		{
			//BaseSelection selection = new RouletteSelection();
			BaseCrossover crossover = new NormalCrossover();
			BaseMutation mutation = new IVMutation();
			RunGeneticAlgorithm(10, "pla33810.tsp", 1000, 30, 1, 0.07, 0.3, mutation, crossover);
			RunGeneticAlgorithm(10, "pla85900.tsp", 1000, 30, 1, 0.07, 0.3, mutation, crossover);
			RunGeneticAlgorithm(10, "usa13509.tsp", 1500, 50, 1, 0.07, 0.4, mutation, crossover);

		}


		public static void RunGeneticAlgorithm(int iterations, string file_name, int maxT, int populationSize, double crossoverProbability, double k1, double k2, BaseMutation mutation, BaseCrossover crossover)
		{
			var inputReader = new InputReader("InputFiles\\" + file_name);
			inputReader.ReadInput();

			List<Task> tasks = new List<Task>();
			int i;

			meanValue = 0;
			values = new List<double>();

			meanTimestamp = 0;
			Timestamps = new List<long>();

			for (i = 0; i < iterations; ++i)
			{
				tasks.Add(Task.Factory.StartNew(() => calc(inputReader.Nodes, maxT, populationSize, crossoverProbability, k1, k2, mutation, crossover)));
				if ((i + 1) % 5 == 0)
				{
					Task.WaitAll(tasks.ToArray());
					tasks.Clear();
				}
			}

			Task.WaitAll(tasks.ToArray());
			
			meanValue /= i;
			double SDValues = 0;
			for (int j = 0; j < i; ++j)
			{
				SDValues += Math.Pow(values[j] - meanValue, 2);
			}

			SDValues = Math.Sqrt(SDValues / i);


			meanTimestamp /= i;

			Record best = new Record() {
				FileName = file_name,
				AvgValue = meanValue,
				SdValue = SDValues,
				BestValue = values.Min(),
				AvgTime = meanTimestamp,
				K1Prob = k1,
				CrossProb = crossoverProbability,
				MutType = mutation.GetType().Name,
				CrossType = crossover.GetType().Name
			};

			WriteToDB(best, "Records");
		}
		

		static void calc(Dictionary<int ,(double x, double y)> Nodes,int maxT, int populationSize, double crossoverProbability, double k1, double k2, BaseMutation mutation, BaseCrossover crossover)
		{
			var TimestampStart = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

			(_, double dd) = GeneticAlgorithmAdaptive.RunAdaptive(Nodes, maxT, populationSize, crossoverProbability, k1, k2, mutation, crossover);

			//double dd = GeneticAlgorithm.GetMin(s_function, dimensions, digitsOfprecision, 2000, 200, mutateProbability, crossoverProbability);

			Console.WriteLine("Value: " + dd);

			var TimestampFinish = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

			lock (sumlock)
			{
				Timestamps.Add(TimestampFinish - TimestampStart);
				meanTimestamp += TimestampFinish - TimestampStart;

				meanValue += dd;
				values.Add(dd);
			}
		}


		private static void WriteToDB(Record record, string table)
		{
			string insertSql = $"INSERT INTO {table} (file_name, avg_value, sd_value, best_value, avg_time, k1_prob, cross_prob, mutation_type, crossover_type) VALUES (@fileName, @avgValue, @sdValue, @bestValue, @avgTime, @k1Prob, @crossProb, @mutationType, @crossoverType)";

			using (connection)
			{
				using (SQLiteCommand command = new SQLiteCommand(insertSql, connection))
				{
					command.Parameters.AddWithValue("@fileName", record.FileName);
					command.Parameters.AddWithValue("@avgValue", record.AvgValue);
					command.Parameters.AddWithValue("@sdValue", record.SdValue);
					command.Parameters.AddWithValue("@bestValue", record.BestValue);
					command.Parameters.AddWithValue("@avgTime", record.AvgTime);
					command.Parameters.AddWithValue("@k1Prob", record.K1Prob);
					command.Parameters.AddWithValue("@crossProb", record.CrossProb);
					command.Parameters.AddWithValue("@mutationType", record.MutType);
					command.Parameters.AddWithValue("@crossoverType", record.CrossType);

					connection.Open();
					command.ExecuteNonQuery();
					connection.Close();
				}
			}
		}
	}
}