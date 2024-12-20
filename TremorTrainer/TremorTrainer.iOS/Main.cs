﻿using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using Syncfusion.XForms.iOS.PopupLayout;
using UIKit;

namespace TremorTrainer.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            SfPopupLayoutRenderer.Init();
            UIApplication.Main(args, null, typeof(AppDelegate));
        }

    }
}
