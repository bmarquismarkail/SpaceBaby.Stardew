using Microsoft.Xna.Framework;

namespace SpaceBaby.RadialMenu.Framework
{
    public class FadeHelper {
        public FadeHelper(int fadeTime, int visibilityTime)
        {
            this.VisibilityTime = visibilityTime;
            this.FadeTime = fadeTime;
        }

        public int VisibilityTime { get;}
        public int FadeTime { get;}

        public float GetAlpha(int ticksRemaining) {
        
        if (ticksRemaining > FadeTime)
            return 1f; // No fade out

        float alpha = ticksRemaining / (float)FadeTime;
        // Ease out curve: smoother fade
        return MathHelper.Clamp(alpha * alpha, 0f, 1f);
        }
    };
}
