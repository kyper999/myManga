﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace myManga_App.IO.File_System
{
    public class SmartFileAccess
    {
        protected readonly SynchronizationContext synchronizationContext;

        public SmartFileAccess()
        {
            synchronizationContext = SynchronizationContext.Current;
        }
    }
}
