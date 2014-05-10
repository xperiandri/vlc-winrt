﻿/**********************************************************************
 * VLC for WinRT
 **********************************************************************
 * Copyright © 2013-2014 VideoLAN and Authors
 *
 * Licensed under GPLv2+ and MPLv2
 * Refer to COPYING file of the official project for license
 **********************************************************************/

using VLC_WINRT.Common;
using VLC_WINRT.ViewModels;
#if WINDOWS_PHONE_APP

#endif
using VLC_WINRT_APP;

namespace VLC_WINRT.Utility.Commands
{
    public class StopVideoCommand : AlwaysExecutableCommand
    {
        public override void Execute(object parameter)
        {
            Locator.PlayVideoVM.UnRegisterMediaControlEvents();
#if NETFX_CORE
            App.RootPage.MainFrame.GoBack();
#endif
        }
    }
}
