// Version 1.4
// ©2013 Reindeer Games
// All rights reserved
// Redistribution of source code without permission not allowed

//#define DBG
//#define PROFILING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Exploder.Core;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Exploder
{
    /// <summary>
    /// main Exploder class
    /// usage:
    /// 1) assign this class to any GameObject (empty or with mesh)
    /// 2) adjust parameters (radius, number of fragments, force, ...)
    /// 3) call function Explode()
    /// 4) wait one or more frames for calculating the explosion
    /// 5) see the explosion
    /// 7) DONE
    /// </summary>
    public class ExploderObject : MonoBehaviour
    {
        /// <summary>
        /// name of the tag for destroyable objects
        /// only objects with this tag can be destroyed, other objects are ignored
        /// </summary>
        public static string Tag = "Exploder";

        /// <summary>
        /// flag for not tagging Explodable objects
        /// if you set this to TRUE you will have to assign "Explodable" to your GameObject instead of Tagging it
        /// this is useful if you already have tagged GameObject and you don't want to re-tag it to "Exploder"
        /// </summary>
        public bool DontUseTag = false;

        /// <summary>
        /// radius of explosion
        /// see red wire-frame sphere inside scene view
        /// </summary>
        public float Radius = 10;

        /// <summary>
        /// cubic radius of explosion
        /// see red wire-frame cube inside scene view
        /// </summary>
        public Vector3 CubeRadius = Vector3.zero;

        /// <summary>
        /// flag for using cubic radius
        /// </summary>
        public bool UseCubeRadius = false;

        /// <summary>
        /// vector of explosion force
        /// NOTE: this parameter is used only if "settings.UseForceVector == true"
        /// ex.: with Vector(0, 0, 1) exploding fragments will fly in "UP" direction
        /// </summary>
        public Vector3 ForceVector = Vector3.up;

        /// <summary>
        /// flag for using "ForceVector"
        /// if this flag is false explosion force is distributed randomly on unit sphere (from sphere center to all directions)
        /// </summary>
        public bool UseForceVector;

        /// <summary>
        /// force of explosion
        /// more means higher velocity of exploding fragments
        /// </summary>
        public float Force = 30;

        /// <summary>
        /// time budget in [ms] for processing explosion calculation in one frame
        /// if the calculation takes more time it is stopped and resumed in next frame
        /// recommended settings: 15 - 30 (30 frame-per-second game takes approximately 33ms in one frame)
        /// for example:
        /// if your game is running 30 fps in average this value should be lower than 30 (~ 15 can be ok)
        /// if your game is running 60 fps in average this value can be 30 and more
        /// in other words, higher the value is faster the calculation is finished but more time in one frame can take
        /// </summary>
        public float FrameBudget = 15;

        /// <summary>
        /// number of target fragments that will be created by cutting the exploding objects
        /// more fragments means more calculation and more PhysX overhead
        /// </summary>
        public int TargetFragments = 30;

        /// <summary>
        /// deactivate options for fragment pieces
        /// </summary>
        public DeactivateOptions DeactivateOptions = DeactivateOptions.Never;

        /// <summary>
        /// deactivate timeout, valid only if DeactivateOptions == DeactivateTimeout
        /// </summary>
        public float DeactivateTimeout = 10.0f;

        /// <summary>
        /// options for fading out fragments after explosion
        /// </summary>
        public FadeoutOptions FadeoutOptions = FadeoutOptions.None;

        /// <summary>
        /// using mesh colliders for fragments
        /// NOTE: don't use it unless you have to, mesh colliders are very slow
        /// </summary>
        public bool MeshColliders = false;

        /// <summary>
        /// flag for destroying this GameObject if there is any mesh
        /// </summary>
        public bool ExplodeSelf = true;

        /// <summary>
        /// disable scanning for explodable objects in radius
        /// this options is valid only if ExplodeSelf is true
        /// </summary>
        public bool DisableRadiusScan = false;

        /// <summary>
        /// flag for hiding this game object after explosion
        /// </summary>
        public bool HideSelf = true;

        /// <summary>
        /// flag for destroying game object after explosion
        /// </summary>
        public bool DestroyOriginalObject = false;

        /// <summary>
        /// flag for destroying already destroyed fragments
        /// if this is true you can destroy object and all the new created fragments
        /// you can keep destroying fragments until they are small enough (see Fragment.cs)
        /// </summary>
        public bool ExplodeFragments = true;

        /// <summary>
        /// by enabling this Exploder will handle all objets in radius equally
        /// they will have the same number of fragments
        /// </summary>
        public bool UniformFragmentDistribution = false;

        /// <summary>
        /// option for separating not-connecting parts of the same mesh
        /// if this option is enabled all exploding fragments are searched for not connecting 
        /// parts of the same mesh and these parts are separated into new fragments
        /// example:
        /// if you explode a "chair" model, mesh cutter cut it into pieces however it is likely
        /// possible that one of the fragments will contain not-connecting "chair legs" (no sitting part) 
        /// and it will look not very realistic, by enabling this all not connecting "chair legs" are found 
        /// and split into different meshes
        /// 
        /// IMPORTANT: by enabling this you can achieve better visual quality but it will take more CPU power
        /// (more frames to process the explosion)
        /// </summary>
        public bool SplitMeshIslands = false;

        /// <summary>
        /// by enabling this option exploder will also cut non-closed meshes, for example vehicle with windows (mesh with holes)
        /// however it might not triangulate properly the mesh because of the original hole
        /// use this option with models that doesn't explode or only explode in very few fragments
        /// </summary>
        public bool AllowOpenMeshCutting;

        /// <summary>
        /// maximum number of all available fragments
        /// this number should be higher than TargetFragments
        /// </summary>
        public int FragmentPoolSize = 200;

        /// <summary>
        /// custom game object will be instantiated as a fragment
        /// </summary>
        public GameObject FragmentPrefab = null;

        /// <summary>
        /// if enabled this will use 2d collision rigid bodies, valid only from Unity 4.3
        /// </summary>
        public bool Use2DCollision;

        [Serializable]
        public class SFXOption
        {
            public AudioClip ExplosionSoundClip;
            public AudioClip FragmentSoundClip;
            public GameObject FragmentEmitter;
            public float HitSoundTimeout;
            public int EmitersMax;
            public float ParticleTimeout;

            public SFXOption Clone()
            {
                return new SFXOption
                {
                    ExplosionSoundClip = ExplosionSoundClip,
                    FragmentSoundClip = FragmentSoundClip,
                    FragmentEmitter = FragmentEmitter,
                    HitSoundTimeout = HitSoundTimeout,
                    EmitersMax = EmitersMax,
                    ParticleTimeout = ParticleTimeout,
                };
            }
        }

        public SFXOption SFXOptions = new SFXOption
        {
            ExplosionSoundClip = null,
            FragmentSoundClip = null,
            FragmentEmitter = null,
            HitSoundTimeout = 0.3f,
            EmitersMax = 1000,
            ParticleTimeout = -1.0f,
        };

        [Serializable]
        public class FragmentOption
        {
            public bool FreezePositionX;
            public bool FreezePositionY;
            public bool FreezePositionZ;
            public bool FreezeRotationX;
            public bool FreezeRotationY;
            public bool FreezeRotationZ;

            public string Layer;

            /// <summary>
            /// maximal velocity the fragment can fly
            /// </summary>
            public float MaxVelocity;

            /// <summary>
            /// if set to true, mass, velocity and angular velocity will be inherited from original game object
            /// </summary>
            public bool InheritParentPhysicsProperty;

            /// <summary>
            /// mass property which will apply to fragments
            /// NOTE: if the parent object object has rigidbody and InheritParentPhysicsProperty is true
            /// the mass property for fragments will be calculated based on this equation (fragmentMass = parentMass / settings.TargetFragments)
            /// </summary>
            public float Mass;

            /// <summary>
            /// gravity settings
            /// </summary>
            public bool UseGravity;

            /// <summary>
            /// disable collider on fragments
            /// </summary>
            public bool DisableColliders;

            /// <summary>
            /// angular velocity of fragments
            /// </summary>
            public float AngularVelocity;

            /// <summary>
            /// maximal angular velocity of fragment
            /// </summary>
            public float MaxAngularVelocity;

            /// <summary>
            /// direction of angular velocity
            /// </summary>
            public Vector3 AngularVelocityVector;

            /// <summary>
            /// set this to true if you want to have randomly rotated fragments
            /// </summary>
            public bool RandomAngularVelocityVector;

            /// <summary>
            /// optional parameter to use different material for fragment pieces
            /// if not set the default (first) material is chosen from the original object
            /// </summary>
            public Material FragmentMaterial;

            public FragmentOption Clone()
            {
                return new FragmentOption
                {
                    FreezePositionX = FreezePositionX,
                    FreezePositionY = FreezePositionY,
                    FreezePositionZ = FreezePositionZ,
                    FreezeRotationX = FreezeRotationX,
                    FreezeRotationY = FreezeRotationY,
                    FreezeRotationZ = FreezeRotationZ,
                    Layer = Layer,
                    Mass = Mass,
                    DisableColliders = DisableColliders,
                    UseGravity = UseGravity,
                    MaxVelocity = MaxVelocity,
                    MaxAngularVelocity = MaxAngularVelocity,
                    InheritParentPhysicsProperty = InheritParentPhysicsProperty,
                    AngularVelocity = AngularVelocity,
                    AngularVelocityVector = AngularVelocityVector,
                    RandomAngularVelocityVector = RandomAngularVelocityVector,
                    FragmentMaterial = FragmentMaterial,
                };
            }
        }

        /// <summary>
        /// global settings for fragment options
        /// constrains for rigid bodies and name of the layer
        /// </summary>
        public FragmentOption FragmentOptions = new FragmentOption
        {
            FreezePositionX = false,
            FreezePositionY = false,
            FreezePositionZ = false,
            FreezeRotationX = false,
            FreezeRotationY = false,
            FreezeRotationZ = false,
            Layer = "Default",
            Mass = 20,
            MaxVelocity = 1000,
            DisableColliders = false,
            UseGravity = true,
            InheritParentPhysicsProperty = true,
            AngularVelocity = 1.0f,
            AngularVelocityVector = Vector3.up,
            MaxAngularVelocity = 7,
            RandomAngularVelocityVector = true,
            FragmentMaterial = null,
        };

        internal GameObject Target;

        /// <summary>
        /// explosion callback
        /// this callback is called when the calculation is finished and the physics explosion is started
        /// this is useful for playing explosion sound effect, particles etc.
        /// </summary>
        public delegate void OnExplosion(float timeMS, ExplosionState state);

        /// <summary>
        /// state of explosion, this enum used as parameter in callback
        /// </summary>
        public enum ExplosionState
        {
            /// <summary>
            /// explosion just started to show flying fragment pieces, but it can take several frames to
            /// start all pieces (activate rigid bodies, etc...)
            /// this is a good place to play explosion soundeffects
            /// </summary>
            ExplosionStarted,

            /// <summary>
            /// explosion process is finally completed, all fragment pieces are generated and visible
            /// this is a good place to get all active fragments and do watever necessery (particles, FX, ...)
            /// </summary>
            ExplosionFinished,
        }

        /// <summary>
        /// search and explode objects in radius
        /// </summary>
        public void ExplodeRadius()
        {
            ExplodeRadius(null);
        }

        /// <summary>
        /// search and explode objects in radius
        /// </summary>
        /// <param name="callback">callback to be called when explosion calculation is finished
        /// play your sound effects or particles on this callback
        /// </param>
        public void ExplodeRadius(OnExplosion callback)
        {
            queue.Enqueue(callback, null);
        }

        /// <summary>
        /// explode single object
        /// </summary>
        /// <param name="obj">game object to be exploded</param>
        public void ExplodeObject(GameObject obj)
        {
            ExplodeObject(obj, null);
        }

        /// <summary>
        /// explode single object with callback
        /// </summary>
        /// <param name="obj">game object to be exploded</param>
        /// <param name="callback">callback to be called when explosion calculation is finished
        /// play your sound effects or particles on this callback</param>
        public void ExplodeObject(GameObject obj, OnExplosion callback)
        {
            queue.Enqueue(callback, obj);
        }

        /// <summary>
        /// callback from queue, do not call this unles you know what you are doing!
        /// </summary>
        public void StartExplosionFromQueue(ExploderSettings settings)
        {
            ExploderUtils.Assert(state == State.None, "Wrong state: " + state);

			this.settings = settings;
            state = State.Preprocess;

#if DBG
        processingTime = 0.0f;
        preProcessingTime = 0.0f;
        postProcessingTime = 0.0f;
        postProcessingTimeEnd = 0.0f;
        isolatingIslandsTime = 0.0f;
#endif
        }

        /// <summary>
        /// cracking callback
        /// this callback is called when the explosion calculation is finished and the objects are ready for explosion
        /// </summary>
        public delegate void OnCracked();

        /// <summary>
        /// crack will calculate fragments and prepare object for explosion
        /// Use this method in combination with ExplodeCracked()
        /// Purpose of this method is to get higher performance of explosion, Crack() will 
        /// calculate the explosion and prepare all fragments. Calling ExplodeCracked() will 
        /// then start the explosion (flying fragments...) immediately
        /// </summary>
        public void CrackRadius()
        {
            CrackRadius(null);
        }

        /// <summary>
        /// crack will calculate fragments and prepare object for explosion
        /// Use this method in combination with ExplodeCracked()
        /// Purpose of this method is to get higher performance of explosion, Crack() will 
        /// calculate the explosion and prepare all fragments. Calling ExplodeCracked() will 
        /// then start the explosion (flying fragments...) immediately
        /// </summary>
        public void CrackRadius(OnCracked callback)
        {
            ExploderUtils.Assert(!crack, "Another crack in progress!");

            if (!crack)
            {
                CrackedCallback = callback;
                crack = true;
                cracked = false;
                ExplodeRadius(null);
            }
        }

        /// <summary>
        /// crack single object, use in combination with ExplodeCracked(...)
        /// </summary>
        /// <param name="obj">object to be cracked</param>
        public void CrackObject(GameObject obj)
        {
            CrackObject(obj, null);
        }

        /// <summary>
        /// crack single object, use in combination with ExplodeCracked(...)
        /// </summary>
        /// <param name="obj">object to be cracked</param>
        /// <param name="callback"></param>
        public void CrackObject(GameObject obj, OnCracked callback)
        {
            ExploderUtils.Assert(!crack, "Another crack in progress!");

            if (!crack)
            {
                CrackedCallback = callback;
                crack = true;
                cracked = false;
                ExplodeObject(obj, null);
            }
        }

        /// <summary>
        /// explode cracked objects
        /// Use this method in combination with Crack()
        /// Purpose of this method is to get higher performance of explosion, Crack() will
        /// calculate the explosion and prepare all fragments. Calling ExplodeCracked() will
        /// then start the explosion (flying fragments...) immediately
        /// </summary>
        public void ExplodeCracked(OnExplosion callback)
        {
            ExploderUtils.Assert(crack, "You must call Crack() first!");

            if (cracked)
            {
                PostCrackExplode(callback);
                crack = false;
            }
        }

        /// <summary>
        /// explode cracked objects
        /// Use this method in combination with Crack()
        /// Purpose of this method is to get higher performance of explosion, Crack() will 
        /// calculate the explosion and prepare all fragments. Calling ExplodeCracked() will 
        /// run the explosion immediately.
        /// </summary>
        public void ExplodeCracked()
        {
            ExplodeCracked(null);
        }

        private OnCracked CrackedCallback;
        private bool crack;
        private bool cracked;

		private ExploderSettings settings;

        enum State
        {
            None,
            Preprocess,
            ProcessCutter,
            IsolateMeshIslands,
            PostprocessInit,
            Postprocess,
            DryRun,
        }

        private State state;
        private ExploderQueue queue;
        private Core.MeshCutter cutter;
        private Stopwatch timer;

        private HashSet<CutMesh> newFragments;
        private HashSet<CutMesh> meshToRemove;
        private HashSet<CutMesh> meshSet;
        private int[] levelCount;

        private int poolIdx;
        private List<CutMesh> postList;
        private List<Fragment> pool;
        private Vector3 crackedPos;
        private Quaternion crackedRot;

        private bool splitMeshIslands;
        private List<CutMesh> islands;

        private AudioSource audioSource;

#if DBG

    private int processingFrames = 0;
    private int postProcessingFrames = 0;
    private int isolatingIslandsFrames = 0;

    private float processingTime = 0.0f;
    private float preProcessingTime = 0.0f;
    private float postProcessingTime = 0.0f;
    private float postProcessingTimeEnd = 0.0f;
    private float isolatingIslandsTime = 0.0f;

#endif

        private struct CutMesh
        {
            public Mesh mesh;
            public Material material;
            public Transform transform;

            public Transform parent;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 localScale;

            public GameObject original;
            public Vector3 centroid;
            public float distance;
            public int vertices;
            public int level;
            public int fragments;

            public ExploderOption option;

            public GameObject skinnedOriginal;
        }

        private void Awake()
        {
            // init cutter
            cutter = new Core.MeshCutter();
            cutter.Init(512, 512);
            UnityEngine.Random.seed = System.DateTime.Now.Millisecond;

            settings= new ExploderSettings(this);

            var use2d = settings.Use2DCollision;

            // init pool
            FragmentPool.Instance.Allocate(settings.FragmentPoolSize, settings.MeshColliders, use2d, settings.FragmentPrefab);
            FragmentPool.Instance.SetDeactivateOptions(settings.DeactivateOptions, settings.FadeoutOptions, settings.DeactivateTimeout);
            FragmentPool.Instance.SetExplodableFragments(settings.ExplodeFragments, settings.DontUseTag);
            FragmentPool.Instance.SetFragmentPhysicsOptions(settings.FragmentOptions, use2d);
            FragmentPool.Instance.SetSFXOptions(settings.SFXOptions);
            timer = new Stopwatch();

            // init queue
            queue = new ExploderQueue(this);

            if (settings.ExplodeSelf)
            {
                if (settings.DontUseTag)
                {
                    gameObject.AddComponent<Explodable>();
                }
                else
                {
                    gameObject.tag = "Exploder";
                }
            }

            state = State.DryRun;

            PreAllocateBuffers();

            state = State.None;

            if (settings.SFXOptions.ExplosionSoundClip)
            {
                audioSource = gameObject.GetComponent<AudioSource>();

                if (!audioSource)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }

        void PreAllocateBuffers()
        {
            // kick memory allocator for better performance at startup

            newFragments = new HashSet<CutMesh>();
            meshToRemove = new HashSet<CutMesh>();
            meshSet = new HashSet<CutMesh>();

            for (int i = 0; i < 64; i++)
            {
                meshSet.Add(new CutMesh());
            }

            levelCount = new int[64];

            // run dummy preprocess to run more allocations...
            Preprocess();
            long t;
            ProcessCutter(out t);
        }

        void OnDrawGizmos()
        {
            if (enabled && !(ExplodeSelf && DisableRadiusScan))
            {
                Gizmos.color = Color.red;

                if (UseCubeRadius)
                {
					var pos = ExploderUtils.GetCentroid(gameObject);
					Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(transform.InverseTransformPoint(pos), CubeRadius);
                }
                else
                {
                    Gizmos.DrawWireSphere(ExploderUtils.GetCentroid(gameObject), Radius);
                }
            }
        }

        private bool IsInRadius(GameObject o)
        {
            var centroid = ExploderUtils.GetCentroid(o);

            if (settings.UseCubeRadius)
            {
                var localP = transform.InverseTransformPoint(centroid);
                var localC = transform.InverseTransformPoint(settings.Position);

                return (Mathf.Abs(localP.x - localC.x) < settings.CubeRadius.x &&
                        Mathf.Abs(localP.y - localC.y) < settings.CubeRadius.y &&
				        Mathf.Abs(localP.z - localC.z) < settings.CubeRadius.z);
            }
            else
            {
                return settings.Radius*settings.Radius > (centroid - settings.Position).sqrMagnitude;
            }
        }

        private int GetLevelFragments(int level, int fragmentsMax)
        {
            return (fragmentsMax * 2) / (level * level + level) + 1;
        }

        private int GetLevel(float distance, float radius)
        {
            ExploderUtils.Assert(distance >= 0.0f, "");
            ExploderUtils.Assert(radius >= 0.0f, "");

            var normDistance = distance / radius * 6;
            var level = (int)normDistance / 2 + 1;

            return Mathf.Clamp(level, 0, 10);
        }

        struct MeshData
        {
            public Mesh sharedMesh;
            public Material sharedMaterial;
            public GameObject gameObject;
            public GameObject parentObject;
            public GameObject skinnedBakeOriginal;
            public Vector3 centroid;
        }

        private List<MeshData> GetMeshData(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<MeshRenderer>();
            var meshFilters = obj.GetComponentsInChildren<MeshFilter>();

            ExploderUtils.Warning(renderers.Length == meshFilters.Length, "Renderers and meshes don't match!");

            if (renderers.Length != meshFilters.Length)
            {
                return new List<MeshData>();
            }

            var outList = new List<MeshData>(renderers.Length);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (meshFilters[i].sharedMesh == null)
                {
                    ExploderUtils.Log("Missing shared mesh in " + meshFilters[i].name);
                    continue;
                }

                if (!meshFilters[i].sharedMesh || !meshFilters[i].sharedMesh.isReadable)
                {
                    UnityEngine.Debug.LogWarning("Mesh is not readable: " + meshFilters[i].name);
                    continue;
                }

                if (/*IsExplodable(meshFilters[i].gameObject)*/true)
                {
                    outList.Add(new MeshData
                    {
                        sharedMesh = meshFilters[i].sharedMesh,
                        sharedMaterial = renderers[i].sharedMaterial,
                        gameObject = renderers[i].gameObject,
                        centroid = renderers[i].bounds.center,
                        parentObject = obj,
                    });
                }
            }

            // find skinned mesh
            var renderersSkinned = obj.GetComponentsInChildren<SkinnedMeshRenderer>();

            for (int i = 0; i < renderersSkinned.Length; i++)
            {
/*
            if (!IsExplodable(renderersSkinned[i].gameObject))
            {
                continue;
            }
*/

                var bakeMesh = new Mesh();
                renderersSkinned[i].BakeMesh(bakeMesh);
                var bakeObj = new GameObject("BakeSkin");
                var meshFilter = bakeObj.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = bakeMesh;
                var meshRenderer = bakeObj.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = renderersSkinned[i].material;
                bakeObj.transform.position = obj.transform.position;
                bakeObj.transform.rotation = obj.transform.rotation;
                ExploderUtils.SetVisible(bakeObj, false);

                outList.Add(new MeshData
                {
                    sharedMesh = bakeMesh,
                    sharedMaterial = meshRenderer.sharedMaterial,
                    gameObject = bakeObj,
                    centroid = meshRenderer.bounds.center,
                    parentObject = bakeObj,
                    skinnedBakeOriginal = obj,
                });
            }

            return outList;
        }

        bool IsExplodable(GameObject obj)
        {
            if (settings.DontUseTag)
            {
                return obj.GetComponent<Explodable>() != null;
            }
            else
            {
                return obj.CompareTag(ExploderObject.Tag);
            }
        }

        private List<CutMesh> GetMeshList()
        {
            GameObject[] objects = null;

            if (settings.Target)
            {
                objects = new[] {settings.Target};
            }
            else
            {
                if (settings.DontUseTag)
                {
                    var objs = FindObjectsOfType(typeof(Explodable));
                    var objList = new List<GameObject>(objs.Length);

                    foreach (var o in objs)
                    {
                        var ex = (Explodable)o;

                        if (ex)
                        {
                            objList.Add(ex.gameObject);
                        }
                    }

                    objects = objList.ToArray();
                }
                else
                {
                    objects = GameObject.FindGameObjectsWithTag("Exploder");
                }
            }

            var list = new List<CutMesh>(objects.Length);

            foreach (var o in objects)
            {
                // don't destroy the destroyer :)
                if (!settings.ExplodeSelf && o == gameObject)
                {
                    continue;
                }

                // stop scanning for object is case of settings.ExplodeSelf
                if (o != gameObject && settings.ExplodeSelf && settings.DisableRadiusScan)
                {
                    continue;
                }

                if (settings.Target || IsInRadius(o))
                {
                    var meshData = GetMeshData(o);
                    var meshDataLen = meshData.Count;

                    for (var i = 0; i < meshDataLen; i++)
                    {
                        var centroid = meshData[i].centroid;

                        // overwrite settings.Position in case of settings.Target
                        if (settings.Target)
                        {
                            settings.Position = centroid;
                        }

                        var distance = (centroid - settings.Position).magnitude;

//                    UnityEngine.Debug.Log("Distance: " + distance + " " + meshData[i].gameObject.name);

                        list.Add(new CutMesh
                        {
                            mesh = meshData[i].sharedMesh,
                            material = meshData[i].sharedMaterial,
                            centroid = meshData[i].gameObject.transform.InverseTransformPoint(centroid),
                            vertices = meshData[i].sharedMesh.vertexCount,
                            transform = meshData[i].gameObject.transform,

                            parent = meshData[i].gameObject.transform.parent,
                            position = meshData[i].gameObject.transform.position,
                            rotation = meshData[i].gameObject.transform.rotation,
                            localScale = meshData[i].gameObject.transform.localScale,

                            distance = distance,
                            level = GetLevel(distance, settings.Radius),
                            original = meshData[i].parentObject,
                            skinnedOriginal = meshData[i].skinnedBakeOriginal,

                            option = o.GetComponent<ExploderOption>(),
                        });
                    }
                }
            }

            if (list.Count == 0)
            {
#if DBG
            ExploderUtils.Log("No explodable objects found!");
#endif
                return list;
            }

            list.Sort(delegate(CutMesh m0, CutMesh m1)
            {
                return (m0.level).CompareTo(m1.level);
            });

            // for the case when the count of objects is higher then target fragments
            if (list.Count > settings.TargetFragments)
            {
                list.RemoveRange(settings.TargetFragments - 1, list.Count - settings.TargetFragments);
            }

            var levelMax = list[list.Count - 1].level;
            var fragmentsPerLevel = GetLevelFragments(levelMax, settings.TargetFragments);

            int maxCount = 0;
            var listCount = list.Count;

            var levelCount = new int[levelMax + 1];
            foreach (var cutMesh in list)
            {
                levelCount[cutMesh.level]++;
            }

            for (int i = 0; i < listCount; i++)
            {
                var cutMesh = list[i];

                var curLevelRatio = levelMax + 1 - cutMesh.level;

                var fragments = (int)((curLevelRatio * fragmentsPerLevel) / levelCount[cutMesh.level]);

                cutMesh.fragments = fragments;

                maxCount += fragments;

                list[i] = cutMesh;

                if (maxCount >= settings.TargetFragments)
                {
                    cutMesh.fragments -= maxCount - settings.TargetFragments;
                    maxCount -= maxCount - settings.TargetFragments;

                    list[i] = cutMesh;

                    break;
                }
            }

//        foreach (var cutMesh in list)
//        {
//            UnityEngine.Debug.Log(cutMesh.level + " " + cutMesh.distance + " " + cutMesh.fragments);
//        }

            return list;
        }

        void Update()
        {
            long cuttingTime = 0;

            switch (state)
            {
                case State.None:
                    break;

                case State.Preprocess:
                    {
                        timer.Reset();
                        timer.Start();
#if DBG
                var watch = new Stopwatch();
                watch.Start();
#endif

                        // get mesh data from game object in radius
                        var readyToCut = Preprocess();

#if DBG
                watch.Stop();
                preProcessingTime = watch.ElapsedMilliseconds;
                processingFrames = 0;
#endif

                        // nothing to explode
                        if (!readyToCut)
                        {
#if DBG
                    ExploderUtils.Log("Nothing to explode "  + settings.Position);
#endif
                            OnExplosionFinished(false);
                        }
                        else
                        {
                            // continue to cutter, don't wait to another frame
                            state = State.ProcessCutter;
                            goto case State.ProcessCutter;
                        }
                    }
                    break;

                case State.ProcessCutter:
                    {
#if DBG
                processingFrames++;
                var watch = new Stopwatch();
                watch.Start();
#endif

                        // process main cutter
                        var cuttingFinished = ProcessCutter(out cuttingTime);

#if DBG
                watch.Stop();
                processingTime += watch.ElapsedMilliseconds;
#endif

                        if (cuttingFinished)
                        {
                            poolIdx = 0;
                            postList = new List<CutMesh>(meshSet);

                            // continue to next state if the cutter is finished
                            if (splitMeshIslands)
                            {
#if DBG
                        isolatingIslandsFrames = 0;
                        isolatingIslandsTime = 0.0f;
#endif
                                islands = new List<CutMesh>(meshSet.Count);

                                state = State.IsolateMeshIslands;
                                goto case State.IsolateMeshIslands;
                            }

                            state = State.PostprocessInit;
                            goto case State.PostprocessInit;
                        }
                    }
                    break;

                case State.IsolateMeshIslands:
                    {
#if DBG
                var watchPost = new Stopwatch();
                watchPost.Start();
#endif

                        var isolatingFinished = IsolateMeshIslands(ref cuttingTime);

#if DBG
                watchPost.Stop();
                isolatingIslandsFrames++;
                isolatingIslandsTime += watchPost.ElapsedMilliseconds;
#endif

                        if (isolatingFinished)
                        {
                            // goto next state
                            state = State.PostprocessInit;
                            goto case State.PostprocessInit;
                        }
                    }
                    break;

                case State.PostprocessInit:
                    {
                        InitPostprocess();

                        state = State.Postprocess;
                        goto case State.Postprocess;
                    }

                case State.Postprocess:
                    {
#if DBG
                var watchPost = new Stopwatch();
                watchPost.Start();
#endif
                        if (Postprocess(cuttingTime))
                        {
                            timer.Stop();
                        }
#if DBG
                watchPost.Stop();
                postProcessingTime += watchPost.ElapsedMilliseconds;
#endif
                    }
                    break;
            }

#if DBG
        if (Input.GetKeyDown(KeyCode.M))
        {
            settings.MeshColliders = !settings.MeshColliders;
        }
#endif
        }

        bool Preprocess()
        {
            //        Exploder.Profiler.Start("Preprocess - GetMeshList");

            // find meshes around exploder centroid
            var meshList = GetMeshList();

            //        Exploder.Profiler.End("Preprocess - GetMeshList");

            // nothing do destroy
            if (meshList.Count == 0)
            {
                return false;
            }

            //        Exploder.Profiler.Start("Preprocess - allocation");

            newFragments.Clear();
            meshToRemove.Clear();
            meshSet = new HashSet<CutMesh>(meshList);

            splitMeshIslands = settings.SplitMeshIslands;

            var levelMax = meshList[meshList.Count - 1].level;
            levelCount = new int[levelMax + 1];

            //        Exploder.Profiler.End("Preprocess - allocation");

            foreach (var meshCut in meshSet)
            {
                levelCount[meshCut.level] += meshCut.fragments;
            }

//            ExploderUtils.ClearLog();
//            foreach (var cutMesh in meshList)
//            {
//                UnityEngine.Debug.Log(cutMesh.level);
//            }

            //
            // apply uniform distribution of fragments
            //
            if (settings.UniformFragmentDistribution)
            {
                var buff = new int[64];
                foreach (var cutMesh in meshSet)
                {
                    buff[cutMesh.level] += 1;
                }

                var fragmentPerObject = settings.TargetFragments / meshSet.Count;

                foreach (var cutMesh in meshSet)
                {
                    levelCount[cutMesh.level] = fragmentPerObject * buff[cutMesh.level];
                }
            }

            return true;
        }

        bool ProcessCutter(out long cuttingTime)
        {
            ExploderUtils.Assert(state == State.ProcessCutter || state == State.DryRun, "Wrong state!");

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            bool cutting = true;
            bool timeBudgetStop = false;
            var cycleCounter = 0;

            while (cutting)
            {
                cycleCounter++;

                if (cycleCounter > settings.TargetFragments)
                {
                    ExploderUtils.Log("Explode Infinite loop!");
                    break;
                }

                var fragmentsCount = meshSet.Count;

                newFragments.Clear();
                meshToRemove.Clear();

                cutting = false;

                foreach (var mesh in meshSet)
                {
                    if (levelCount[mesh.level] > 0)
                    {
                        // TODO: for possible improvements change random value to something more sofisticated
                        var randomPlaneNormal = Random.insideUnitSphere;

                        if (!mesh.transform)
                        {
                            continue;
                        }

                        var plane = new Core.Math.Plane(randomPlaneNormal, mesh.transform.TransformPoint(mesh.centroid));

                        var triangulateHoles = true;
                        var crossSectionVertexColour = Color.white;
                        var crossSectionUV = new Vector4(0, 0, 1, 1);

                        if (mesh.option)
                        {
                            triangulateHoles = !mesh.option.Plane2D;
                            crossSectionVertexColour = mesh.option.CrossSectionVertexColor;
                            crossSectionUV = mesh.option.CrossSectionUV;
                            splitMeshIslands |= mesh.option.SplitMeshIslands;
                        }

                        if (settings.Use2DCollision)
                        {
                            triangulateHoles = false;
                        }

                        List<CutterMesh> meshes = null;
                        cutter.Cut(mesh.mesh, mesh.transform, plane, triangulateHoles, settings.AllowOpenMeshCutting, ref meshes, crossSectionVertexColour, crossSectionUV);

                        cutting = true;

                        if (meshes != null)
                        {
                            foreach (var cutterMesh in meshes)
                            {
                                newFragments.Add(new CutMesh
                                {
                                    mesh = cutterMesh.mesh,
                                    centroid = cutterMesh.centroid,

                                    material = mesh.material,
                                    vertices = mesh.vertices,
                                    transform = mesh.transform,
                                    distance = mesh.distance,
                                    level = mesh.level,
                                    fragments = mesh.fragments,
                                    original = mesh.original,
                                    skinnedOriginal = mesh.skinnedOriginal,

                                    parent = mesh.transform.parent,
                                    position = mesh.transform.position,
                                    rotation = mesh.transform.rotation,
                                    localScale = mesh.transform.localScale,

                                    option = mesh.option,
                                });
                            }

                            meshToRemove.Add(mesh);

                            levelCount[mesh.level] -= 1;

                            // stop this madness!
                            if (fragmentsCount + newFragments.Count - meshToRemove.Count >= settings.TargetFragments)
                            {
                                cuttingTime = stopWatch.ElapsedMilliseconds;
                                meshSet.ExceptWith(meshToRemove);
                                meshSet.UnionWith(newFragments);
                                return true;
                            }

                            // computation took more than settings.FrameBudget ... 
                            if (stopWatch.ElapsedMilliseconds > settings.FrameBudget)
                            {
                                timeBudgetStop = true;
                                break;
                            }
                        }
                    }
                }

                meshSet.ExceptWith(meshToRemove);
                meshSet.UnionWith(newFragments);

                if (timeBudgetStop)
                {
                    break;
                }
            }

            cuttingTime = stopWatch.ElapsedMilliseconds;

            // explosion is finished
            if (!timeBudgetStop)
            {
                return true;
            }

            return false;
        }

        bool IsolateMeshIslands(ref long timeOffset)
        {
            var timer = new Stopwatch();
            timer.Start();

            var count = postList.Count;

            while (poolIdx < count)
            {
                var mesh = postList[poolIdx];
                poolIdx++;

                var islandsFound = false;

                if (settings.SplitMeshIslands || (mesh.option && mesh.option.SplitMeshIslands))
                {
                    var meshIslands = MeshUtils.IsolateMeshIslands(mesh.mesh);

                    if (meshIslands != null)
                    {
                        islandsFound = true;

                        foreach (var meshIsland in meshIslands)
                        {
                            islands.Add(new CutMesh
                            {
                                mesh = meshIsland.mesh,
                                centroid = meshIsland.centroid,

                                material = mesh.material,
                                vertices = mesh.vertices,
                                transform = mesh.transform,
                                distance = mesh.distance,
                                level = mesh.level,
                                fragments = mesh.fragments,
                                original = mesh.original,
                                skinnedOriginal = mesh.skinnedOriginal,

                                parent = mesh.transform.parent,
                                position = mesh.transform.position,
                                rotation = mesh.transform.rotation,
                                localScale = mesh.transform.localScale,

                                option = mesh.option,
                            });
                        }
                    }
                }

                if (!islandsFound)
                {
                    islands.Add(mesh);
                }

                if (timer.ElapsedMilliseconds + timeOffset > settings.FrameBudget)
                {
                    return false;
                }
            }

#if DBG
        ExploderUtils.Log("Replacing fragments: " + postList.Count + " by islands: " + islands.Count);
#endif

            // replace postList by island list
            postList = islands;

            return true;
        }

        void InitPostprocess()
        {
            var fragmentsNum = postList.Count;
            var use2d = settings.Use2DCollision;

            FragmentPool.Instance.Allocate(fragmentsNum, settings.MeshColliders, use2d, settings.FragmentPrefab);
            FragmentPool.Instance.SetDeactivateOptions(settings.DeactivateOptions, settings.FadeoutOptions, settings.DeactivateTimeout);
            FragmentPool.Instance.SetExplodableFragments(settings.ExplodeFragments, settings.DontUseTag);
            FragmentPool.Instance.SetFragmentPhysicsOptions(settings.FragmentOptions, use2d);
            FragmentPool.Instance.SetSFXOptions(settings.SFXOptions);

            poolIdx = 0;
            pool = FragmentPool.Instance.GetAvailableFragments(fragmentsNum);

            if (settings.Callback != null)
            {
                settings.Callback(timer.ElapsedMilliseconds, ExplosionState.ExplosionStarted);
            }

            // run sfx
            if (settings.SFXOptions.ExplosionSoundClip)
            {
                if (!audioSource)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }

                audioSource.PlayOneShot(settings.SFXOptions.ExplosionSoundClip);
            }

            if (crack)
            {
                FragmentPool.Instance.ResetTransform();

                if (postList.Count > 0)
                {
                    if (postList[0].skinnedOriginal)
                    {
                        crackedPos = postList[0].skinnedOriginal.transform.position;
                        crackedRot = postList[0].skinnedOriginal.transform.rotation;
                    }
                    else
                    {
                        crackedPos = postList[0].original.transform.position;
                        crackedRot = postList[0].original.transform.rotation;
                    }
                }
            }

#if DBG
        postProcessingFrames = 0;
#endif
        }

        void PostCrackExplode(OnExplosion callback)
        {
            if (callback != null)
            {
                callback(0.0f, ExplosionState.ExplosionStarted);
            }

            var count = postList.Count;
            poolIdx = 0;

            var diffPos = Vector3.zero;
            var diffRot = Quaternion.identity;

            if (postList.Count > 0)
            {
                if (postList[0].skinnedOriginal)
                {
                    diffPos = postList[0].skinnedOriginal.transform.position - crackedPos;
                    diffRot = postList[0].skinnedOriginal.transform.rotation * Quaternion.Inverse(crackedRot);
                }
                else
                {
                    diffPos = postList[0].original.transform.position - crackedPos;
                    diffRot = postList[0].original.transform.rotation * Quaternion.Inverse(crackedRot);
                }
            }

            while (poolIdx < count)
            {
                var fragment = pool[poolIdx];
                var mesh = postList[poolIdx];

                poolIdx++;

                if (mesh.original != gameObject)
                {
                    ExploderUtils.SetActiveRecursively(mesh.original, false);
                }
                else
                {
                    ExploderUtils.EnableCollider(mesh.original, false);
                    ExploderUtils.SetVisible(mesh.original, false);
                }

                if (mesh.skinnedOriginal && mesh.skinnedOriginal != gameObject)
                {
                    ExploderUtils.SetActiveRecursively(mesh.skinnedOriginal, false);
                }
                else
                {
                    ExploderUtils.EnableCollider(mesh.skinnedOriginal, false);
                    ExploderUtils.SetVisible(mesh.skinnedOriginal, false);
                }

                fragment.transform.position += diffPos;
                fragment.transform.rotation *= diffRot;

                fragment.Explode();
            }

            if (settings.DestroyOriginalObject)
            {
                foreach (var mesh in postList)
                {
                    if (mesh.original && !mesh.original.GetComponent<Fragment>())
                    {
                        Object.Destroy(mesh.original);
                    }

                    if (mesh.skinnedOriginal)
                    {
                        Object.Destroy(mesh.skinnedOriginal);
                    }
                }
            }

            if (settings.ExplodeSelf)
            {
                if (!settings.DestroyOriginalObject)
                {
                    ExploderUtils.SetActiveRecursively(gameObject, false);
                }
            }

            if (settings.HideSelf)
            {
                ExploderUtils.SetActiveRecursively(gameObject, false);
            }

#if DBG
        ExploderUtils.Log("Crack finished! " + postList.Count + postList[0].original.transform.gameObject.name);
#endif
            settings.Callback = callback;
            OnExplosionFinished(true);
        }

        bool Postprocess(long timeOffset)
        {
            var postTimer = new Stopwatch();
            postTimer.Start();

            var count = postList.Count;

#if DBG
        postProcessingFrames++;
#endif

            while (poolIdx < count)
            {
                var fragment = pool[poolIdx];
                var mesh = postList[poolIdx];

                poolIdx++;

                if (!mesh.original)
                {
                    continue;
                }

                if (crack)
                {
                    ExploderUtils.SetActiveRecursively(fragment.gameObject, false);
                }

                fragment.meshFilter.sharedMesh = mesh.mesh;

                // choose proper material

                if (mesh.option && mesh.option.FragmentMaterial)
                {
                    fragment.meshRenderer.sharedMaterial = mesh.option.FragmentMaterial;
                }
                else
                {
                    if (settings.FragmentOptions.FragmentMaterial != null)
                    {
                        fragment.meshRenderer.sharedMaterial = settings.FragmentOptions.FragmentMaterial;
                    }
                    else
                    {
                        fragment.meshRenderer.sharedMaterial = mesh.material;
                    }
                }

                mesh.mesh.RecalculateBounds();

                var oldParent = fragment.transform.parent;
                fragment.transform.parent = mesh.parent;
                fragment.transform.position = mesh.position;
                fragment.transform.rotation = mesh.rotation;
                fragment.transform.localScale = mesh.localScale;
                fragment.transform.parent = null;
                fragment.transform.parent = oldParent;

                if (!crack)
                {
                    if (mesh.original != gameObject)
                    {
                        ExploderUtils.SetActiveRecursively(mesh.original, false);
                    }
                    else
                    {
                        ExploderUtils.EnableCollider(mesh.original, false);
                        ExploderUtils.SetVisible(mesh.original, false);
                    }

                    if (mesh.skinnedOriginal && mesh.skinnedOriginal != gameObject)
                    {
                        ExploderUtils.SetActiveRecursively(mesh.skinnedOriginal, false);
                    }
                    else
                    {
                        ExploderUtils.EnableCollider(mesh.skinnedOriginal, false);
                        ExploderUtils.SetVisible(mesh.skinnedOriginal, false);
                    }
                }

                var plane = mesh.option && mesh.option.Plane2D;

                var use2d = settings.Use2DCollision;

                if (!settings.FragmentOptions.DisableColliders)
                {
                    if (settings.MeshColliders && !use2d)
                    {
                        // dont use mesh colliders for 2d plane
                        if (!plane)
                        {
                            fragment.meshCollider.sharedMesh = mesh.mesh;
                        }
                    }
                    else
                    {

                        if (settings.Use2DCollision)
                        {
                            Core.MeshUtils.GeneratePolygonCollider(fragment.polygonCollider2D, mesh.mesh);
                        }
                        else
                        {
                            fragment.boxCollider.center = mesh.mesh.bounds.center;
                            fragment.boxCollider.size = mesh.mesh.bounds.extents;
                        }
                    }
                }

                if (mesh.option)
                {
                    mesh.option.DuplicateSettings(fragment.options);
                }

                if (!crack)
                {
                    fragment.Explode();
                }

                var force = settings.Force;
                if (mesh.option && mesh.option.UseLocalForce)
                {
                    force = mesh.option.Force;
                }

                // apply force to rigid body
                fragment.ApplyExplosion(mesh.transform, mesh.centroid, settings.Position, settings.FragmentOptions, settings.UseForceVector,
                                        settings.ForceVector, force, mesh.original, settings.TargetFragments);

#if SHOW_DEBUG_LINES
            UnityEngine.Debug.DrawLine(settings.Position, forceVector * settings.Force, Color.yellow, 3);
#endif

                if (postTimer.ElapsedMilliseconds + timeOffset > settings.FrameBudget)
                {
                    return false;
                }
            }

#if DBG
        var watch = new Stopwatch();
        watch.Start();
#endif

            if (!crack)
            {
                if (settings.DestroyOriginalObject)
                {
                    foreach (var mesh in postList)
                    {
                        if (mesh.original && !mesh.original.GetComponent<Fragment>())
                        {
                            Object.Destroy(mesh.original);
                        }

                        if (mesh.skinnedOriginal)
                        {
                            Object.Destroy(mesh.skinnedOriginal);
                        }
                    }
                }

                if (settings.ExplodeSelf)
                {
                    if (!settings.DestroyOriginalObject)
                    {
                        ExploderUtils.SetActiveRecursively(gameObject, false);
                    }
                }

                if (settings.HideSelf)
                {
                    ExploderUtils.SetActiveRecursively(gameObject, false);
                }

#if DBG
            ExploderUtils.Log("Explosion finished! " + postList.Count + postList[0].original.transform.gameObject.name);
#endif
                OnExplosionFinished(true);
            }
            else
            {
                cracked = true;

                if (CrackedCallback != null)
                {
                    CrackedCallback();
                }
            }

#if DBG
        postProcessingTimeEnd = watch.ElapsedMilliseconds;
#endif

            return true;
        }

        void OnExplosionFinished(bool success)
        {
            if (settings.Callback != null)
            {
                if (!success)
                {
                    settings.Callback(timer.ElapsedMilliseconds, ExplosionState.ExplosionStarted);
                    OnExplosionStarted();
                }

                settings.Callback(timer.ElapsedMilliseconds, ExplosionState.ExplosionFinished);
            }

            state = State.None;

            queue.OnExplosionFinished(settings.id);
        }

        void OnExplosionStarted()
        {
        }

#if DBG
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 50, 300, 30), "Explosion time: " + timer.ElapsedMilliseconds + " [ms]");

        GUI.Label(new Rect(10, 80, 500, 30), "Preprocessing time: " + preProcessingTime + " [ms]");
        GUI.Label(new Rect(10, 100, 500, 30), "Processing time AVG: " + processingTime / processingFrames + " [ms]" + " frames: " + processingFrames);
        GUI.Label(new Rect(10, 120, 500, 30), "Isolating islands: " + isolatingIslandsTime / isolatingIslandsFrames + " [ms]" + " frames: " + isolatingIslandsFrames);
        GUI.Label(new Rect(10, 140, 500, 30), "Postprocessing time: " + postProcessingTime / postProcessingFrames + " [ms] " + " postFrames: " + postProcessingFrames);
        GUI.Label(new Rect(10, 160, 500, 30), "Postprocessing time end: " + postProcessingTimeEnd + " [ms] ");

#if PROFILING
        var results = Exploder.Profiler.PrintResults();
        var y = 180;

        foreach (var result in results)
        {
            GUI.Label(new Rect(10, y, 500, 30), "Profiler: " + result);
            y += 20;
        }
#endif

    }
#endif
    }
}
