using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary
{
    public class CustomGraphicsDeviceManager : GraphicsDeviceManager
    {

        private const float WideScreenRatio = 1.6f; //1.77777779f;

        private bool isWideScreenOnly;

        public CustomGraphicsDeviceManager(Game game)
            : base(game)
        {
        }

        public bool IsWideScreenOnly
        {
            get { return isWideScreenOnly; }
            set { isWideScreenOnly = value; }
        }

/*
        protected override void RankDevices(List<GraphicsDeviceInformation> foundDevices)
        {
            base.RankDevices(foundDevices);
            if (IsWideScreenOnly)
            {
                for (int i = 0; i < foundDevices.Count; )
                {
                    PresentationParameters pp = foundDevices[i].PresentationParameters;
                    if (pp.IsFullScreen == true)
                    {
                        float aspectRatio = (float)(pp.BackBufferWidth) / (float)(pp.BackBufferHeight);

                        // If the device does not have a widescreen aspect ratio, remove it.
                        if (aspectRatio < WideScreenRatio)
                        {
                            foundDevices.RemoveAt(i);
                        }
                        else { i++; }
                    }
                    else i++;
                }
            }
        }
*/
    }
}