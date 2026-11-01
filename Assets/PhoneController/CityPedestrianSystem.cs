using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace FlyHigh.PhoneController
{
    [DisallowMultipleComponent]
    public sealed class CityPedestrianSystem : MonoBehaviour
    {
        private readonly List<PedestrianWalker> walkers=new List<PedestrianWalker>();
        private readonly Dictionary<int,List<Vector3>> regions=new Dictionary<int,List<Vector3>>();
        private NavMeshDataInstance navigation;
        private PedestrianSettings settings;
        public PedestrianSurfaces Surfaces { get; private set; }
        public IReadOnlyList<PedestrianWalker> Walkers => walkers;
        public int RoadSafetyCorrections { get; internal set; }
        public void Initialize(Transform player)
        {
            if(settings!=null)return;
            settings=Resources.Load<PedestrianSettings>("FlyHighPedestrians");
            if(settings==null||settings.navigation==null||settings.characters==null||settings.characters.Length==0||settings.waypoints==null){Debug.LogWarning("[Pedestrians] Bake pedestrian routes from Fly High > City > Bake pedestrian routes.");return;}
            navigation=NavMesh.AddNavMeshData(settings.navigation);Surfaces=new PedestrianSurfaces();
            foreach(var point in settings.waypoints){if(!regions.TryGetValue(point.region,out var list)){list=new List<Vector3>();regions.Add(point.region,list);}list.Add(point.position);}
            var chosen=new List<Vector3>();
            Spawn(false,settings.cityPopulation,player!=null?player.position:Vector3.zero,chosen);
            Spawn(true,settings.beachPopulation,player!=null?player.position:Vector3.zero,chosen);
        }
        private void Spawn(bool beach,int count,Vector3 player,List<Vector3> chosen)
        {
            for(int index=0;index<count;index++)
            {
                int best=-1;float bestScore=-1;
                for(int i=0;i<settings.waypoints.Length;i++)
                {
                    var point=settings.waypoints[i];if(point.beach!=beach||regions[point.region].Count<3)continue;
                    float spacing=float.MaxValue;foreach(var other in chosen)spacing=Mathf.Min(spacing,(point.position-other).sqrMagnitude);
                    if(spacing<16)continue;
                    float score=chosen.Count==0?1/(1+(point.position-player).sqrMagnitude):spacing;
                    if(score>bestScore){bestScore=score;best=i;}
                }
                if(best<0)break;
                var start=settings.waypoints[best];chosen.Add(start.position);
                var root=new GameObject((beach?"Beach walker ":"City walker ")+(walkers.Count+1));root.transform.SetParent(transform,false);root.transform.position=start.position;
                var walker=root.AddComponent<PedestrianWalker>();
                var model=settings.characters[walkers.Count%settings.characters.Length];
                walker.Initialize(this,model,regions[start.region],start.region,beach,settings.characterScale,Random.Range(settings.walkSpeed.x,settings.walkSpeed.y),walkers.Count);
                walkers.Add(walker);
            }
        }
        private void OnDestroy(){foreach(var walker in walkers)if(walker!=null)Destroy(walker.gameObject);if(navigation.valid)navigation.Remove();}
    }
}
