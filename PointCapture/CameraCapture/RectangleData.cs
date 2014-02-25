using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CameraCapture
{
/*
 *  Simple object to represent info of each detected object
 */
    class RectangleData
    {
        private int x;
        private int y;
        private int width;
        private int height;
        
        public RectangleData(int init_X, int init_Y, int init_Width, int init_Height)
        {
            this.x = init_X;
            this.y = init_Y;
            this.width = init_Width;
            this.height = init_Height;
        }

        public int getX()
        {
            return this.x;
        }

        public int getY()
        {
            return this.y;
        }

        public int getWidth()
        {
            return this.width;
        }

        public int getHeight()
        {
            return this.height;
        }

        public int getXCenter()
        {
            return x + width / 2;
        }

        public int getYCenter()
        {
            return y + height / 2;
        }
    }
}
