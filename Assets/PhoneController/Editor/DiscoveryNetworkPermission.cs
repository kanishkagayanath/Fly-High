#if UNITY_ANDROID
using System.IO;
using System.Xml.Linq;
using UnityEditor.Android;

namespace FlyHigh.PhoneController.Editor
{
    /// <summary>Allows reading the active LAN's addresses without Wi-Fi identity or location access.</summary>
    public sealed class DiscoveryNetworkPermission : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifest = Path.Combine(path, "src/main/AndroidManifest.xml");
            var document = XDocument.Load(manifest);
            XNamespace android = "http://schemas.android.com/apk/res/android";
            foreach (var permission in document.Root.Elements("uses-permission"))
                if ((string)permission.Attribute(android + "name") == "android.permission.ACCESS_NETWORK_STATE")
                    return;
            document.Root.Add(new XElement("uses-permission",
                new XAttribute(android + "name", "android.permission.ACCESS_NETWORK_STATE")));
            document.Save(manifest);
        }
    }
}
#endif
