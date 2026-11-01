using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlyHigh.PhoneController.Editor
{
    public static class CoinMissionBuilder
    {
        private static Material PickupMaterial(string path,GameObject model,Color emission,bool textureEmission)
        {
            var source=model.GetComponentInChildren<Renderer>().sharedMaterial;
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(source);AssetDatabase.CreateAsset(material,path);}
            material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
            material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",emission);
            if(textureEmission && source.mainTexture!=null)material.SetTexture("_EmissionMap",source.mainTexture);
            EditorUtility.SetDirty(material);return material;
        }
        [MenuItem("Fly High/City/Bake coin missions")]
        public static void Bake()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "CityDrive")
                throw new System.InvalidOperationException("Open CityDrive and stop Play before baking coin missions.");
            var coin=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/Coin.fbx");
            var sack=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/Coin_sack/goldbag.fbx");
            if(coin==null || sack==null)throw new System.InvalidOperationException("Imported Coin and goldbag models are required.");
            Physics.SyncTransforms();
            var graph=new CityRoadGraph();graph.Build();
            if(graph.nodes.Count<20)throw new System.InvalidOperationException("Connected asphalt roads were not found.");
            var random=Random.state;
            var routes=new List<CoinMissionRoute>();
            try
            {
                Random.InitState(61429);
                var spawn=Object.FindFirstObjectByType<CityCarSpawn>();
                Vector3 origin=spawn!=null?spawn.transform.position:new Vector3(10,.7f,79.5f);
                var preferences=new[]{origin+Vector3.left*18,new Vector3(-58,.45f,-60),new Vector3(54,.45f,48)};
                var titles=new[]{"Coastal Coin Run","Downtown Gold Trail","City Explorer"};
                for(int m=0;m<preferences.Length;m++)
                {
                    var candidates=new List<int>();for(int i=0;i<graph.nodes.Count;i++)if(graph.nodes[i].neighbours.Count>=3)candidates.Add(i);
                    Vector3 preference=preferences[m];candidates.Sort((a,b)=>(graph.nodes[a].position-preference).sqrMagnitude.CompareTo((graph.nodes[b].position-preference).sqrMagnitude));
                    TrafficPath chosen=null;int tried=0;
                    foreach(int start in candidates)
                    {
                        if(tried++>=12)break;
                        bool spaced=true;foreach(var existing in routes)if(Vector3.Distance(existing.path[0],graph.nodes[start].position)<40)spaced=false;
                        if(!spaced)continue;
                        var route=new List<int>();
                        foreach(int neighbour in graph.nodes[start].neighbours)
                        {
                            Vector3 from=graph.nodes[start].position,heading=(graph.nodes[neighbour].position-from).normalized;
                            Vector3 lead=from+heading*2;
                            var half=new Vector3(1.05f,.33f,2f);
                            if(!graph.ClearSegment(from,lead,half))continue;
                            int district=(graph.District(from)+4)%9;
                            for(int attempt=0;attempt<9;attempt++)
                            {
                                if(!graph.PlanRoute(graph.NearestNode(lead),heading,ref district,titles[m],route))continue;
                                var path=TrafficPath.Build(graph,from,lead,route,half);
                                if(path!=null && path.Length>=115 && (chosen==null || path.Length>chosen.Length))chosen=path;
                                if(chosen!=null && chosen.Length>=180)break;
                            }
                            if(chosen!=null && chosen.Length>=180)break;
                        }
                        if(chosen!=null)break;
                    }
                    if(chosen==null)throw new System.InvalidOperationException("No safe road route for "+titles[m]+"; original mission asset preserved.");
                    var points=new List<Vector3>();
                    for(float distance=0;distance<chosen.Length;distance+=1){chosen.Evaluate(distance,out var p,out _);points.Add(p);}
                    chosen.Evaluate(chosen.Length,out var end,out _);points.Add(end);
                    var coins=new List<Vector3>();
                    for(float distance=7;distance<chosen.Length-5;distance+=7){chosen.Evaluate(distance,out var p,out _);coins.Add(p);}
                    routes.Add(new CoinMissionRoute{title=titles[m],path=points.ToArray(),coins=coins.ToArray(),distance=chosen.Length});
                }
            }
            finally{Random.state=random;}
            using(var lanes=new CoinLanePlacement(graph))
                for(int i=0;i<routes.Count;i++)routes[i]=lanes.Align(routes[i]);
            const string folder="Assets/PhoneController/Resources";
            const string pathSettings=folder+"/FlyHighMissions.asset";
            const string materialPath=folder+"/MissionGold.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.name="Mission Gold";material.color=new Color(1,.76f,.21f);AssetDatabase.CreateAsset(material,materialPath);}
            var settings=AssetDatabase.LoadAssetAtPath<CoinMissionSettings>(pathSettings);
            if(settings==null){settings=ScriptableObject.CreateInstance<CoinMissionSettings>();AssetDatabase.CreateAsset(settings,pathSettings);}
            settings.coinModel=coin;settings.sackModel=sack;settings.markerMaterial=material;settings.routes=routes.ToArray();
            settings.coinMaterial=PickupMaterial(folder+"/MissionCoin.mat",coin,new Color(.8f,.42f,.012f),false);
            settings.sackMaterial=PickupMaterial(folder+"/MissionSack.mat",sack,new Color(.28f,.23f,.17f),true);
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
            Debug.Log("[Missions] Baked "+routes.Count+" road-only coin routes. Scene unchanged.");
        }
    }
}




