﻿// -----------------------------------------------------------------------
// <copyright file="MonitoringTaskFactory.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using PerfTap.Configuration;
	using PerfTap.Counter;
	using PerfTap.Net;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class MonitoringTaskFactory
	{
		private ICounterConfiguration _counterConfig;
		private IReportingConfiguration _reportingConfig;
		private List<string> _counterPaths;

		/// <summary>
		/// Initializes a new instance of the MonitoringTaskFactory class.
		/// </summary>
		/// <param name="counterConfig"></param>
		public MonitoringTaskFactory(ICounterConfiguration counterConfig, IReportingConfiguration reportingConfig)
		{
			if (null == counterConfig) { throw new ArgumentNullException("counterConfig"); }
			if (null == reportingConfig) { throw new ArgumentNullException("reportingConfig"); }

			_counterConfig = counterConfig;
			_counterPaths = counterConfig.DefinitionPaths
				.SelectMany(path => CounterFileParser.ReadCountersFromFile(path))
				.ToList();
			_reportingConfig = reportingConfig;
		}

		public Task CreateTask(CancellationToken cancellationToken)
		{
			return new Task(() => 
				{
					using (var reader = new PerfmonCounterReader())
					{
						//TODO: this doesn't quite jive yet -- need to grab timespan from configuration
						var metrics = reader.StreamCounterSamples(_counterPaths, TimeSpan.FromSeconds(1), cancellationToken)
							.SelectMany(set => set.CounterSamples.ToGraphiteString(_reportingConfig.Key));

						using (var messenger = new UdpMessenger(_reportingConfig.Server, _reportingConfig.Port))
						{
							messenger.SendMetrics(metrics);
						}						
					}
				}, cancellationToken);
			/*
			
		#when querying at a 10 second or less interval, batch into at least 10-second groups to cut down on cpu usage
		$maxSamples = 1
		if ($SecondFrequency -lt 10)
		{
			#TODO: should we prevent the timer from overlapping?
			$maxSamples = [Math]::Round(10 / $SecondFrequency)
		}
		*/
		}
	}
}