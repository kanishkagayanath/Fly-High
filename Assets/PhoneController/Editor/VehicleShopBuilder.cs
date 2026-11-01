using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlyHigh.PhoneController.Editor
{
    public static class VehicleShopBuilder
    {
        [MenuItem("Fly High/City/Prepare vehicle shop")]
        public static void Prepare()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play before preparing the vehicle shop.");
            const string folder="Assets/PhoneController/Shop";
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            string carFolder="Assets/ithappy/Cartoon_City_Free/Prefabs/Cars/";
            string[] ids={"starter","hatchback","classic","coupe","van","future"};
            string[] names={"Sunset Sport","City Hatch","Coastal Classic","Gold Coupe","Town Cruiser","Neon Concept"};
            string[] models={"Assets/Vehicles/Car_13.prefab",carFolder+"Car_06.prefab",carFolder+"Car_19.prefab",carFolder+"Car_16.prefab",carFolder+"Van.prefab",carFolder+"Futuristic_Car_1.prefab"};
            string[] descriptions={"Your original city ride","Compact and ready to explore","A classic shape for the coast","Stand out in bright gold","Take the scenic route","A futuristic city silhouette"};
            int[] prices={0,100,180,300,250,450};var entries=new List<ShopVehicle>();
            for(int i=0;i<ids.Length;i++)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(models[i]);if(prefab==null)throw new System.InvalidOperationException("Missing vehicle: "+models[i]);
                string path=folder+"/"+ids[i]+".png";
                var utility=new PreviewRenderUtility(true);
                Texture2D result=null;
                try
                {
                    var visual=Object.Instantiate(prefab);utility.AddSingleGO(visual);
                    foreach(var collider in visual.GetComponentsInChildren<Collider>())collider.enabled=false;
                    var renderers=visual.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                    visual.transform.localScale*=3.6f/bounds.size.z;bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);visual.transform.position-=bounds.center;
                    utility.camera.transform.position=new Vector3(4.9f,2.8f,6.4f);utility.camera.transform.LookAt(Vector3.zero);utility.camera.fieldOfView=31;utility.camera.orthographic=true;utility.camera.orthographicSize=1.65f;utility.camera.nearClipPlane=.1f;utility.camera.farClipPlane=30;
                    utility.camera.clearFlags=CameraClearFlags.SolidColor;utility.camera.backgroundColor=new Color(.025f,.065f,.11f,0);
                    utility.lights[0].intensity=1.35f;utility.lights[0].transform.rotation=Quaternion.Euler(40,35,0);utility.lights[1].intensity=.9f;utility.lights[1].transform.rotation=Quaternion.Euler(330,220,0);utility.ambientColor=new Color(.55f,.55f,.55f);
                    utility.BeginStaticPreview(new Rect(0,0,512,256));utility.Render(true);utility.Render(true);result=utility.EndStaticPreview();File.WriteAllBytes(path,result.EncodeToPNG());
                }
                finally{if(result!=null)Object.DestroyImmediate(result);utility.Cleanup();}
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=512;importer.SaveAndReimport();
                entries.Add(new ShopVehicle{id=ids[i],title=names[i],description=descriptions[i],price=prices[i],prefab=prefab,thumbnail=AssetDatabase.LoadAssetAtPath<Sprite>(path)});
            }
            const string asset="Assets/PhoneController/Resources/FlyHighVehicleShop.asset";
            var catalog=AssetDatabase.LoadAssetAtPath<VehicleShopCatalog>(asset);if(catalog==null){catalog=ScriptableObject.CreateInstance<VehicleShopCatalog>();AssetDatabase.CreateAsset(catalog,asset);}catalog.vehicles=entries.ToArray();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
    }
}

