﻿/**********************************************************************
 * VLC for WinRT
 **********************************************************************
 * Copyright © 2013-2014 VideoLAN and Authors
 *
 * Licensed under GPLv2+ and MPLv2
 * Refer to COPYING file of the official project for license
 **********************************************************************/

namespace VLC_WINRT.Utility.Helpers
{
    public static class MathHelper
    {
        public static double Clamp(int m, int M, int x)
        {
            return (x > M) ? M : (x < m) ? m : x;
        }

        public static float Clamp(float m, float M, float x)
        {
            return (x > M) ? M : (x < m) ? m : x;
        }
    }
}
