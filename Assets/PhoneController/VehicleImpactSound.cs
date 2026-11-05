using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Only horizontal impacts qualify; road support contacts never make crash sounds.</summary>
    public sealed class VehicleImpactSound : MonoBehaviour
    {
        private GameAudio gameAudio;
        public void Initialize(GameAudio owner){gameAudio=owner;}
        private void OnCollisionEnter(Collision hit)
        {
            if(gameAudio==null||hit.contactCount==0)return;
            float speed=0;Vector3 point=transform.position;
            for(int i=0;i<hit.contactCount;i++)
            {
                var contact=hit.GetContact(i);if(Mathf.Abs(contact.normal.y)>.65f)continue;
                float inward=Mathf.Abs(Vector3.Dot(hit.relativeVelocity,contact.normal));
                if(inward>speed){speed=inward;point=contact.point;}
            }
            if(speed>0)gameAudio.PlayImpact(point,speed);
        }
    }
}
