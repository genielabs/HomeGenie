/*
	MIG Input Plugin TUIO Data Structure
    author: Generoso Martello <generoso@martello.com>
    date  : 11-2008
*/

using System;
using System.Collections;

namespace OSC.NET.Implementations.TUIO
{
    public struct TUIOData
    {
        public int f_id;
        public float X, Y, X0, Y0, Angle;
		public ArrayList Alive;
		
        public TUIOData(int id, float x, float y, float X, float Y, float a)
        {
			Alive = null;
            this.f_id = id;
            this.X = x;
            this.Y = y;
			this.X0 = X;
			this.Y0 = Y;
            this.Angle = a;
        }
        
		public TUIOData(ArrayList alive)
		{
			Alive = alive;
            this.f_id = 0;
            this.X = 0;
            this.Y = 0;
			this.X0 = 0;
			this.Y0 = 0;
            this.Angle = 0;
		}
		
        public void update(float x, float y, float a)
        {
            this.X = x;
            this.Y = y;
            this.Angle = a;
        }
    }
}

