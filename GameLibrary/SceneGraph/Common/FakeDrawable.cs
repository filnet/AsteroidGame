using GameLibrary.SceneGraph.Bounding;

namespace GameLibrary.SceneGraph.Common
{
    // Fake is always invisible (but not its bounds...)
    public class FakeDrawable : AbstractDrawable
    {
        public override Volume BoundingVolume
        {
            get;
        }

        public override Volume WorldBoundingVolume
        {
            get
            {
                return BoundingVolume;
            }
        }

        public FakeDrawable(int renderGroupId, Volume boundingVolume)
        {
            Enabled = true;
            Visible = false;
            RenderGroupId = renderGroupId;
            BoundingVolume = boundingVolume;
            BoundingVolumeVisible = true;
        }
    }

}
