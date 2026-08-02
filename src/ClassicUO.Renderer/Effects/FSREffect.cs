using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    public class FSREffect : Effect
    {
        public FSREffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, Resources.GetFSRShader().ToArray())
        {
            MatrixTransform = Parameters["MatrixTransform"];
            TextureSize = Parameters["textureSize"];
        }

        public EffectParameter MatrixTransform { get; }
        public EffectParameter TextureSize { get; }
    }
}
