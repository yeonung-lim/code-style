using AssetKits.ParticleImage;
using UnityEngine;
using UnityEngine.Events;

namespace Effects
{
    public class UIParticle : IEffect
    {
        public UIParticle(ParticleImage effectObject, bool isAutoRelease)
        {
            EffectObject = effectObject;

            if (isAutoRelease)
                EffectObject.main.onLastParticleFinished.AddListener(Release);
        }

        public ParticleImage EffectObject { get; private set; }

        public void Play()
        {
            EffectObject.Play();
        }

        public void Stop()
        {
            EffectObject.Stop();
        }

        public void Release()
        {
            Object.Destroy(EffectObject.gameObject);
            EffectObject = null;
        }

        public Transform Transform => EffectObject.transform;
        public UnityEvent OnFinished => EffectObject.main.onLastParticleFinished;
    }
}