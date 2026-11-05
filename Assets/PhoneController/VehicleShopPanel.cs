using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FlyHigh.PhoneController
{
    public sealed class VehicleShopPanel : MonoBehaviour
    {
        private sealed class Card {public ShopVehicle entry;public TMP_Text price,caption;public UnityEngine.UI.Button action;}
        private VehicleGarage garage;
        private RectTransform root;
        private TMP_Text balance,message;
        private Card[] cards;
        public RectTransform Root => root;
        public UnityEngine.UI.Button BackButton {get;private set;}
        public bool IsOpen => root!=null && root.gameObject.activeSelf;
        public void Initialize(Transform parent,VehicleGarage progress,MenuVisualSettings visuals,Action back)
        {
            garage=progress;
            root=Area(parent,"Vehicle shop",.045f,.055f,.955f,.735f);
            var background=root.gameObject.AddComponent<UnityEngine.UI.Image>();background.color=new Color(.018f,.035f,.065f,.98f);
            Panel(root,"Shop accent",0,.991f,1,1,new Color(.98f,.73f,.24f));
            Label(root,"VEHICLE SHOP",.03f,.9f,.58f,.98f,31,Color.white);
            balance=Label(root,"",.66f,.9f,.97f,.98f,26,new Color(1,.79f,.3f));balance.alignment=TextAlignmentOptions.Right;
            Label(root,"Earn coins in missions. Buy your next ride. Same arcade handling for every car.",.03f,.827f,.97f,.9f,16,new Color(.72f,.81f,.88f));
            var entries=garage.Catalog!=null?garage.Catalog.vehicles:null;
            cards=new Card[entries!=null?entries.Length:0];
            for(int i=0;i<cards.Length;i++)
            {
                var entry=entries[i];int column=i%3,row=i/3;
                float left=.03f+column*.315f,top=.805f-row*.335f;
                var card=Area(root,"Shop vehicle "+entry.id,left,top-.315f,left+.31f,top);
                Panel(card,"Card background",0,0,1,1,new Color(.055f,.095f,.145f));
                Label(card,entry.title.ToUpperInvariant(),.045f,.79f,.96f,.985f,19,Color.white);
                var picture=Area(card,"Vehicle preview",.015f,.23f,.985f,.82f).gameObject.AddComponent<UnityEngine.UI.Image>();picture.sprite=entry.thumbnail;picture.preserveAspect=true;picture.raycastTarget=false;
                var price=Label(card,"",.045f,.035f,.43f,.235f,17,new Color(1,.79f,.3f));
                var action=ActionButton(card,"Vehicle action "+entry.id,.45f,.03f,.965f,.245f,visuals!=null?visuals.playButton:null);
                var caption=action.GetComponentInChildren<TMP_Text>();var captured=entry.id;action.onClick.AddListener(()=>Activate(captured));
                cards[i]=new Card{entry=entry,price=price,caption=caption,action=action};
            }
            BackButton=ActionButton(root,"Shop back",.03f,.025f,.235f,.115f,visuals!=null?visuals.optionsButton:null);BackButton.GetComponentInChildren<TMP_Text>().text="BACK";BackButton.onClick.AddListener(()=>back());
            message=Label(root,"",.265f,.02f,.97f,.12f,16,new Color(.73f,.86f,.91f));
            garage.Changed+=Refresh;Refresh();root.gameObject.SetActive(false);
        }
        public void Show(){root.SetAsLastSibling();root.gameObject.SetActive(true);message.text="";Refresh();}
        public void Hide(){if(root!=null)root.gameObject.SetActive(false);}
        private void Activate(string id)
        {
            string result;bool success=garage.IsOwned(id)?garage.TrySelect(id,out result):garage.TryBuy(id,out result);
            message.text=result;message.color=success?new Color(.4f,1,.79f):new Color(1,.77f,.36f);Refresh();
            if(success && EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(BackButton.gameObject);
        }
        public void ShowResult(string value,bool success){message.text=value;message.color=success?new Color(.4f,1,.79f):new Color(1,.77f,.36f);Refresh();}
        private void Refresh()
        {
            if(balance==null)return;balance.text=garage.Coins.ToString("N0")+" COINS";
            foreach(var card in cards)
            {
                bool owned=garage.IsOwned(card.entry.id),selected=garage.Selected==card.entry,affordable=garage.Coins>=card.entry.price;
                card.price.text=owned?(selected?"SELECTED":"OWNED"):card.entry.price.ToString("N0")+" COINS";
                card.caption.text=selected?"DRIVING":owned?"SELECT":affordable?"BUY":(card.entry.price-garage.Coins).ToString("N0")+" MORE";
                card.action.interactable=garage.CanPurchase && !selected && (owned || affordable);
                card.price.color=selected?new Color(.4f,1,.79f):new Color(1,.79f,.3f);
            }
            if(!garage.CanPurchase)message.text="Garage unavailable. Your saved record has been preserved.";
        }
        private static RectTransform Area(Transform parent,string name,float x,float y,float right,float top)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=new Vector2(x,y);rect.anchorMax=new Vector2(right,top);rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        private static void Panel(Transform parent,string name,float x,float y,float right,float top,Color color)
        {var image=Area(parent,name,x,y,right,top).gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;}
        private static TMP_Text Label(Transform parent,string value,float x,float y,float right,float top,int size,Color color)
        {
            var label=Area(parent,value.Length>0?value:"Shop label",x,y,right,top).gameObject.AddComponent<TextMeshProUGUI>();label.text=value;label.color=color;label.fontSize=size;label.enableAutoSizing=true;label.fontSizeMin=11;label.fontSizeMax=size;label.alignment=TextAlignmentOptions.MidlineLeft;label.raycastTarget=false;return label;
        }
        private static UnityEngine.UI.Button ActionButton(Transform parent,string name,float x,float y,float right,float top,Sprite sprite)
        {
            var rect=Area(parent,name,x,y,right,top);var visual=Area(rect,"Button visual",0,0,1,1);var image=visual.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=sprite;image.color=Color.white;if(sprite!=null){image.type=UnityEngine.UI.Image.Type.Sliced;image.pixelsPerUnitMultiplier=6;}
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();ButtonSound.Attach(button);button.targetGraphic=image;var colors=button.colors;colors.highlightedColor=Color.white;colors.selectedColor=Color.white;colors.pressedColor=new Color(.6f,.8f,.9f);colors.disabledColor=new Color(.35f,.43f,.49f,.8f);button.colors=colors;
            var label=Label(visual,"",.08f,.08f,.92f,.92f,17,Color.white);label.fontStyle=FontStyles.Bold;label.alignment=TextAlignmentOptions.Center;rect.gameObject.AddComponent<MenuButtonMotion>().Initialize(visual);return button;
        }
        private void OnDestroy(){if(garage!=null)garage.Changed-=Refresh;if(root!=null)Destroy(root.gameObject);}
    }
}

