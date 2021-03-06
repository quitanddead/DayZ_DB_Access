﻿using System;
using System.Threading;

namespace DBAccess
{
    //
    //
    //
    public class MapZoom
    {
        public MapZoom(EventWaitHandle evtHandle)
        {
            this.evtHandle = evtHandle;
        }

        public void Start(VirtualMap map, Tool.Point center, int depthDir)
        {
            this.centerUnit = center / map.SizeCorrected;

            this.currDepth = Math.Min(Math.Max(this.currDepth, map.nfo.min_depth), map.nfo.mag_depth - 1);
            this.destDepth = Math.Min(Math.Max(this.destDepth, map.nfo.min_depth), map.nfo.mag_depth - 1);

            int newDepth = this.destDepth + depthDir;

            if ((newDepth >= map.nfo.min_depth) && (newDepth <= map.nfo.mag_depth - 1))
            {
                this.destDepth = newDepth;
                this.evtHandle.Set();
            }
        }

        internal bool Update(VirtualMap map)
        {
            Tool.Point center = (centerUnit * map.SizeCorrected).Truncate;
            Tool.Point newPos;

            double deltaDepth = this.destDepth - this.currDepth;
            if (Math.Abs(deltaDepth) > depthSpeed)
            {
                map.ResizeFromZoom((float)Math.Pow(2, currDepth));

                newPos = (centerUnit * map.SizeCorrected).Truncate;

                map.Position = map.Position - (newPos - center);

                this.currDepth += Math.Sign(deltaDepth) * depthSpeed;

                return true;
            }

            this.currDepth = this.destDepth;
            map.ResizeFromZoom((float)Math.Pow(2, currDepth));

            newPos = (centerUnit * map.SizeCorrected).Truncate;

            map.Position = map.Position - (newPos - center);

            return false;
        }

        public Tool.Point centerUnit;
        public double currDepth = 0;
        public int destDepth = 0;
        public bool IsZooming { get { return Math.Abs(this.destDepth - this.currDepth) > depthSpeed; } }

        private EventWaitHandle evtHandle;
        private static float depthSpeed = 0.08f;
    }
}
