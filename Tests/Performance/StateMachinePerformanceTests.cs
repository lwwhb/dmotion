﻿using System;
using DMotion.Tests;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.PerformanceTesting;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;

namespace DMotion.PerformanceTests
{
    [CreateSystemsForTest(
        typeof(AnimationStateMachineSystem),
        typeof(PlayOneShotSystem),
        typeof(StateMachinePerformanceTestSystem),
        typeof(ClipSamplingSystem),
        typeof(BlendAnimationStatesSystem),
        typeof(UpdateAnimationStatesSystem))]
    public class StateMachinePerformanceTests : PerformanceTestsBase
    {
        [SerializeField, ConvertGameObjectPrefab(nameof(skeletonPrefabEntity))]
        private GameObject skeletonPrefab;

        [SerializeField] private PerformanceTestBenchmarksPerMachine avgUpdateTimeBenchmarks;

        private Entity skeletonPrefabEntity;

        private static int[] testValues = { 1000, 10_000, 100_000 };
        internal static readonly ProfilerMarker Marker =
            new("StateMachinePerformanceTests (UpdateWorld)");

        [Test, Performance]
        public void AverageUpdateTime([ValueSource(nameof(testValues))] int count)
        {
            InstantiateEntities(count, skeletonPrefabEntity);
            DefaultPerformanceMeasure(Marker).Run();

            if (TryGetBenchmarkForCount(count, avgUpdateTimeBenchmarks, out var benchmark))
            {
                benchmark.AssertWithinBenchmark();
            }
        }

        private bool TryGetBenchmarkForCount(int count, PerformanceTestBenchmarksPerMachine groupsAsset,
            out PerformanceTestBenchmark benchmark)
        {
            var groups = groupsAsset.MachineBenchmarks;
            benchmark = default;
            if (groups == null || groups.Length == 0)
            {
                return false;
            }

            var machineName = SystemInfo.deviceName;
            var groupIndex = Array.FindIndex(groups, g => g.MachineName.Equals(machineName));
            if (groupIndex < 0)
            {
                return false;
            }

            var group = groups[groupIndex];

            Assert.IsNotNull(group.Benchmarks);
            var index = Array.FindIndex(group.Benchmarks, b => b.Count == count);
            if (index < 0)
            {
                return false;
            }

            benchmark = group.Benchmarks[index];
            return true;
        }

        private void InstantiateEntities(int count, Entity prefab)
        {
            Assert.IsTrue(manager.HasComponent<LinearBlendDirection>(prefab));
            Assert.IsTrue(manager.HasComponent<PlayOneShotRequest>(prefab));
            Assert.IsTrue(manager.HasComponent<FloatParameter>(prefab));
            Assert.IsTrue(manager.HasComponent<BoolParameter>(prefab));
            Assert.IsTrue(manager.HasComponent<StressTestOneShotClip>(prefab));

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            for (var i = 0; i < count; i++)
            {
                ecb.Instantiate(prefab);
            }
            ecb.Playback(manager);
            ecb.Dispose();

            UpdateWorld();
        }
    }
}