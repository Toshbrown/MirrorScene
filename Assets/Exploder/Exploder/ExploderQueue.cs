// Version 1.4
// ©2013 Reindeer Games
// All rights reserved
// Redistribution of source code without permission not allowed

using System.Collections.Generic;
using UnityEngine;

namespace Exploder
{
    public class ExploderSettings
    {
        public Vector3 Position;
        public Vector3 ForceVector;
        public float Force;
        public float FrameBudget;
        public float Radius;
        public bool UseCubeRadius;
        public Vector3 CubeRadius;
        public float DeactivateTimeout;
        public GameObject Target;
        public int id;
        public int TargetFragments;
        public DeactivateOptions DeactivateOptions;
        public ExploderObject.FragmentOption FragmentOptions;
        public ExploderObject.SFXOption SfxOptions;
        public ExploderObject.OnExplosion Callback;
        public bool DontUseTag;
        public bool UseForceVector;
        public bool MeshColliders;
        public bool ExplodeSelf;
        public bool HideSelf;
        public bool DestroyOriginalObject;
        public bool ExplodeFragments;
        public bool SplitMeshIslands;
        public bool Use2DCollision;
        public int FragmentPoolSize;
        public GameObject FragmentPrefab;
        public FadeoutOptions FadeoutOptions;
        public ExploderObject.SFXOption SFXOptions;
        public bool DisableRadiusScan;
        public bool UniformFragmentDistribution;
        public bool AllowOpenMeshCutting;
        public bool processing;

        public ExploderSettings(ExploderObject exploder)
        {
            Position = ExploderUtils.GetCentroid(exploder.gameObject);
            DontUseTag = exploder.DontUseTag;
            Radius = exploder.Radius;
            UseCubeRadius = exploder.UseCubeRadius;
            CubeRadius = exploder.CubeRadius;
            ForceVector = exploder.ForceVector;
            UseForceVector = exploder.UseForceVector;
            Force = exploder.Force;
            FrameBudget = exploder.FrameBudget;
            TargetFragments = exploder.TargetFragments;
            DeactivateOptions = exploder.DeactivateOptions;
            DeactivateTimeout = exploder.DeactivateTimeout;
            MeshColliders = exploder.MeshColliders;
            ExplodeSelf = exploder.ExplodeSelf;
            HideSelf = exploder.HideSelf;
            DestroyOriginalObject = exploder.DestroyOriginalObject;
            ExplodeFragments = exploder.ExplodeFragments;
            SplitMeshIslands = exploder.SplitMeshIslands;
            FragmentOptions = exploder.FragmentOptions.Clone();
            SfxOptions = exploder.SFXOptions.Clone();
            Use2DCollision = exploder.Use2DCollision;
            FragmentPoolSize = exploder.FragmentPoolSize;
            FragmentPrefab = exploder.FragmentPrefab;
            FadeoutOptions = exploder.FadeoutOptions;
            SFXOptions = exploder.SFXOptions;
            DisableRadiusScan = exploder.DisableRadiusScan;
            UniformFragmentDistribution = exploder.UniformFragmentDistribution;
            AllowOpenMeshCutting = exploder.AllowOpenMeshCutting;
        }
    }

    public class ExploderQueue
    {
        private readonly Queue<ExploderSettings> queue;
        private readonly ExploderObject exploder;

        public ExploderQueue(ExploderObject exploder)
        {
            this.exploder = exploder;
            queue = new Queue<ExploderSettings>();
        }

        public void Enqueue(ExploderObject.OnExplosion callback, GameObject obj)
        {
            var settings = new ExploderSettings(exploder)
            {
                Callback = callback,
                Target = obj,
                processing = false
            };

            queue.Enqueue(settings);
            ProcessQueue();
        }

        void ProcessQueue()
        {
            if (queue.Count > 0)
            {
                var peek = queue.Peek();

                if (!peek.processing)
                {
                    peek.id = Random.Range(int.MinValue, int.MaxValue);
                    peek.processing = true;
                    exploder.StartExplosionFromQueue(peek);
                }
            }
        }

        public void OnExplosionFinished(int id)
        {
            var explosion = queue.Dequeue();
            ExploderUtils.Assert(explosion.id == id, "Explosion id mismatch!");
            ProcessQueue();
        }
    }
}
