using TMPro;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    public sealed class MissionMapOverlay : MonoBehaviour
    {
        private CoinMissionSystem mission;
        private CityMinimap map;
        private RectTransform root, routeRoot, target;
        private RectTransform[] starts, coins;
        private CoinMissionRoute route;
        private const float Radius=93;
        private static readonly Color Gold=new Color(1,.76f,.21f);

        public void Initialize(CoinMissionSystem owner,CityMinimap minimap)
        {
            mission=owner;map=minimap;if(map==null)return;
            root=Element(map.MissionClip,"Mission map markers",Vector2.zero);
            starts=new RectTransform[mission.Settings.routes.Length];
            for(int i=0;i<starts.Length;i++)starts[i]=Icon(root,"M",24,Gold);
            target=Icon(root,"•",19,Gold);target.gameObject.SetActive(false);
        }
        public void Begin(CoinMissionRoute value)
        {
            if(root==null)return;ClearRun();route=value;
            routeRoot=Element(map.MissionContent,"Gold coin route",new Vector2(4096,4096));
            var line=routeRoot.gameObject.AddComponent<MinimapGraphic>();
            for(int i=1;i<route.path.Length;i++)
            {
                Vector2 a=map.MissionPoint(route.path[i-1]),b=map.MissionPoint(route.path[i]);
                Vector2 across=new Vector2(-(b-a).y,(b-a).x).normalized*.8f;
                var tint=new Color(1,.71f,.16f,.52f);
                line.AddTriangle(a-across,a+across,b+across,tint);line.AddTriangle(a-across,b+across,b-across,tint);
            }
            line.Finish();coins=new RectTransform[route.coins.Length];
            for(int i=0;i<coins.Length;i++)
            {coins[i]=Element(routeRoot,"Coin map dot "+(i+1),new Vector2(4,4));coins[i].anchoredPosition=map.MissionPoint(route.coins[i]);coins[i].gameObject.AddComponent<MinimapGraphic>().SetCircle(Gold);}
            var finish=Icon(routeRoot,"F",16,new Color(.32f,1,.8f));finish.anchoredPosition=map.MissionPoint(route.path[route.path.Length-1]);
        }
        public void Pickup(int index){if(coins!=null && index<coins.Length)coins[index].gameObject.SetActive(false);}
        public void ClearRun(){if(routeRoot!=null)Destroy(routeRoot.gameObject);coins=null;route=null;if(target!=null)target.gameObject.SetActive(false);}
        private void LateUpdate()
        {
            if(root==null)return;
            bool exploring=mission.Phase==CoinMissionPhase.Exploring || mission.Phase==CoinMissionPhase.Available;
            for(int i=0;i<starts.Length;i++)
            {starts[i].gameObject.SetActive(exploring);starts[i].anchoredPosition=Clamp(mission.Settings.routes[i].path[0]);}
            bool running=mission.Phase==CoinMissionPhase.Collecting && route!=null;
            target.gameObject.SetActive(running);
            if(running){int next=mission.NextCoin();Vector3 p=next>=0?route.coins[next]:route.path[route.path.Length-1];target.anchoredPosition=Clamp(p);}
        }
        private Vector2 Clamp(Vector3 world)=>Vector2.ClampMagnitude(map.MissionPoint(world)-map.MissionPoint(mission.PlayerPosition),Radius);
        private static RectTransform Element(Transform parent,string name,Vector2 size)
        {var rect=MissionUi.Rect(parent,name,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,size);rect.pivot=Vector2.one*.5f;return rect;}
        private static RectTransform Icon(Transform parent,string label,float size,Color color)
        {
            var rect=Element(parent,"Map "+label,new Vector2(size,size));rect.gameObject.AddComponent<MinimapGraphic>().SetCircle(new Color(.02f,.07f,.1f,.95f));
            var text=Element(rect,"Map symbol",new Vector2(size,size)).gameObject.AddComponent<TextMeshProUGUI>();
            text.text=label;text.fontSize=size*.72f;text.fontStyle=FontStyles.Bold;text.color=color;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
            return rect;
        }
        private void OnEnable(){if(root!=null)root.gameObject.SetActive(true);if(routeRoot!=null)routeRoot.gameObject.SetActive(true);}
        private void OnDisable(){if(root!=null)root.gameObject.SetActive(false);if(routeRoot!=null)routeRoot.gameObject.SetActive(false);}
        private void OnDestroy(){if(root!=null)Destroy(root.gameObject);if(routeRoot!=null)Destroy(routeRoot.gameObject);}
    }
}
